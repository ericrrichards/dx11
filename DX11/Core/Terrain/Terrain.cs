using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using Core.FX;
using SlimDX;
using SlimDX.DXGI;
using SlimDX.Direct3D11;
using Buffer = SlimDX.Direct3D11.Buffer;
using Device = SlimDX.Direct3D11.Device;

namespace Core.Terrain {
    public struct InitInfo {
        // RAW heightmap image file
        public string HeightMapFilename;
        // Heightmap maximum height
        public float HeightScale;
        // Heightmap dimensions
        public int HeightMapWidth;
        public int HeightMapHeight;
        // terrain diffuse textures
        public string LayerMapFilename0;
        public string LayerMapFilename1;
        public string LayerMapFilename2;
        public string LayerMapFilename3;
        public string LayerMapFilename4;
        // Blend map which indicates which diffuse map is
        // applied which portions of the terrain
        public string BlendMapFilename;
        // The distance between vertices in the generated mesh
        public float CellSpacing;
        public Material? Material;
    }

    public class Terrain  :DisposableClass {
        private const int CellsPerPatch = 64;
        private Buffer _quadPatchVB;
        private Buffer _quadPatchIB;

        private ShaderResourceView _layerMapArraySRV;
        private ShaderResourceView _blendMapSRV;
        private ShaderResourceView _heightMapSRV;

        private InitInfo _info;
        private int _numPatchVertices;
        private int _numPatchQuadFaces;

        // number of rows of patch control point vertices
        private int _numPatchVertRows;
        // number of columns of patch control point vertices
        private int _numPatchVertCols;

        public Matrix World { get; set; }

        private Material _material;

        // computed Y bounds for each patch
        private List<Vector2> _patchBoundsY;
        private HeightMap _heightMap;

        private bool _disposed;

        public Terrain() {
            World = Matrix.Identity;
            _material = new Material {
                Ambient = Color.White,
                Diffuse = Color.White,
                Specular = new Color4(64.0f, 0, 0, 0),
                Reflect = Color.Black
            };
        }
        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Util.ReleaseCom(ref _quadPatchVB);
                    Util.ReleaseCom(ref _quadPatchIB);

                    Util.ReleaseCom(ref _layerMapArraySRV);
                    Util.ReleaseCom(ref _blendMapSRV);
                    Util.ReleaseCom(ref _heightMapSRV);
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }
        public float Width { get { return (_info.HeightMapWidth - 1)*_info.CellSpacing; } }
        public float Depth { get { return (_info.HeightMapHeight - 1)*_info.CellSpacing; } }
        public float Height(float x, float z) {
            var c = (x + 0.5f*Width)/_info.CellSpacing;
            var d = (z - 0.5f*Depth)/-_info.CellSpacing;
            var row = (int)Math.Floor(d);
            var col = (int) Math.Floor(c);

            var h00 = _heightMap[row, col];
            var h01 = _heightMap[row, col + 1];
            var h10 = _heightMap[(row +1), col];
            var h11 = _heightMap[(row +1),col + 1];

            var s = c - col;
            var t = d - row;

            if (s + t <= 1.0f) {
                var uy = h01 - h00;
                var vy = h01 - h11;
                return h00 + (1.0f - s)*uy + (1.0f - t)*vy;
            } else {
                var uy = h10 - h11;
                var vy = h01 - h11;
                return h11 + (1.0f - s)*uy + (1.0f-t)*vy;
            }
        }
        public void Init(Device device, DeviceContext dc, InitInfo info) {
            _info = info;
            _numPatchVertRows = ((_info.HeightMapHeight - 1)/CellsPerPatch) + 1;
            _numPatchVertCols = ((_info.HeightMapWidth - 1)/CellsPerPatch) + 1;
            _numPatchVertices = _numPatchVertRows*_numPatchVertCols;
            _numPatchQuadFaces = (_numPatchVertRows - 1)*(_numPatchVertCols - 1);
            
            if (_info.Material.HasValue) {
                _material = _info.Material.Value;
            }
            
            _heightMap = new HeightMap(_info.HeightMapWidth, _info.HeightMapHeight, _info.HeightScale);
            if (!string.IsNullOrEmpty(_info.HeightMapFilename)) {
                _heightMap.LoadHeightmap(_info.HeightMapFilename);
                _heightMap.Smooth();
            } else {
                throw new NotImplementedException("Procedural heightmaps not yet supported");
            }

            CalcAllPatchBoundsY();

            BuildQuadPatchVB(device);
            BuildQuadPatchIB(device);
            _heightMapSRV = _heightMap.BuildHeightmapSRV(device);

            var layerFilenames = new List<string> {
                _info.LayerMapFilename0 ?? "textures/null.bmp",
                _info.LayerMapFilename1 ?? "textures/null.bmp",
                _info.LayerMapFilename2 ?? "textures/null.bmp",
                _info.LayerMapFilename3 ?? "textures/null.bmp",
                _info.LayerMapFilename4 ?? "textures/null.bmp"
            };
            _layerMapArraySRV = Util.CreateTexture2DArraySRV(device, dc, layerFilenames.ToArray(), Format.R8G8B8A8_UNorm);
            if (!string.IsNullOrEmpty(_info.BlendMapFilename)) {
                _blendMapSRV = ShaderResourceView.FromFile(device, _info.BlendMapFilename);
            } else {
                throw new NotImplementedException("Procedural blendmaps not yet supported");
            }
        }
public void Draw(DeviceContext dc, Camera.CameraBase cam, DirectionalLight[] lights) {
    dc.InputAssembler.PrimitiveTopology = PrimitiveTopology.PatchListWith4ControlPoints;
    dc.InputAssembler.InputLayout = InputLayouts.TerrainCP;

    var stride = Vertex.TerrainCP.Stride;
    const int Offset = 0;

    dc.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_quadPatchVB, stride, Offset));
    dc.InputAssembler.SetIndexBuffer(_quadPatchIB, Format.R16_UInt, 0);

    var viewProj = cam.ViewProj;
    var planes = cam.FrustumPlanes;

    Effects.TerrainFX.SetViewProj(viewProj);
    Effects.TerrainFX.SetEyePosW(cam.Position);
    Effects.TerrainFX.SetDirLights(lights);
    Effects.TerrainFX.SetFogColor(Color.Silver);
    Effects.TerrainFX.SetFogStart(15.0f);
    Effects.TerrainFX.SetFogRange(175.0f);
    Effects.TerrainFX.SetMinDist(20.0f);
    Effects.TerrainFX.SetMaxDist(500.0f);
    Effects.TerrainFX.SetMinTess(0.0f);
    Effects.TerrainFX.SetMaxTess(6.0f);
    Effects.TerrainFX.SetTexelCellSpaceU(1.0f/_info.HeightMapWidth);
    Effects.TerrainFX.SetTexelCellSpaceV(1.0f/_info.HeightMapHeight);
    Effects.TerrainFX.SetWorldCellSpace(_info.CellSpacing);
    Effects.TerrainFX.SetWorldFrustumPlanes(planes);
    Effects.TerrainFX.SetLayerMapArray(_layerMapArraySRV);
    Effects.TerrainFX.SetBlendMap(_blendMapSRV);
    Effects.TerrainFX.SetHeightMap(_heightMapSRV);
    Effects.TerrainFX.SetMaterial(_material);

    var tech = Effects.TerrainFX.Light1Tech;
    for (int p = 0; p < tech.Description.PassCount; p++) {
        var pass = tech.GetPassByIndex(p);
        pass.Apply(dc);
        dc.DrawIndexed(_numPatchQuadFaces * 4, 0, 0);
    }
    dc.HullShader.Set(null);
    dc.DomainShader.Set(null);

}
        
        private void BuildQuadPatchIB(Device device) {
            var indices = new List<int>();
            for (int i = 0; i < _numPatchVertRows-1; i++) {
                for (int j = 0; j < _numPatchVertCols; j++) {
                    indices.Add(i*_numPatchVertCols+j);
                    indices.Add(i * _numPatchVertCols + j + 1);
                    indices.Add((i+1) * _numPatchVertCols + j);
                    indices.Add((i+1) * _numPatchVertCols + j + 1);
                }
            }
            var ibd = new BufferDescription(
                sizeof (short)*indices.Count, 
                ResourceUsage.Immutable, 
                BindFlags.IndexBuffer, 
                CpuAccessFlags.None, 
                ResourceOptionFlags.None, 0
            );
            _quadPatchIB = new Buffer(
                device, 
                new DataStream(indices.Select(i=>(short)i).ToArray(), false, false), 
                ibd
            );
        }


        private void CalcAllPatchBoundsY() {
            _patchBoundsY = new List<Vector2>(new Vector2[_numPatchQuadFaces]);

            for (var i = 0; i < _numPatchVertRows-1; i++) {
                for (var j = 0; j < _numPatchVertCols-1; j++) {
                    CalcPatchBoundsY(i, j);
                }
            }
        }

        private void CalcPatchBoundsY(int i, int j) {
            var x0 = j*CellsPerPatch;
            var x1 = (j + 1)*CellsPerPatch;

            var y0 = i*CellsPerPatch;
            var y1 = (i + 1)*CellsPerPatch;

            var minY = float.MaxValue;
            var maxY = float.MinValue;

            for (var y = y0; y <= y1; y++) {
                for (var x = x0; x <= x1; x++) {
                    minY = Math.Min(minY, _heightMap[y,x]);
                    maxY = Math.Max(maxY, _heightMap[y,x]);
                }
            }
            var patchID = i*(_numPatchVertCols - 1) + j;
            _patchBoundsY[patchID] = new Vector2(minY, maxY);
        }

        private void BuildQuadPatchVB(Device device) {
            var patchVerts = new Vertex.TerrainCP[_numPatchVertices];
            var halfWidth = 0.5f*Width;
            var halfDepth = 0.5f*Depth;

            var patchWidth = Width/(_numPatchVertCols - 1);
            var patchDepth = Depth/(_numPatchVertRows - 1);
            var du = 1.0f/(_numPatchVertCols - 1);
            var dv = 1.0f/(_numPatchVertRows - 1);

            for (int i = 0; i < _numPatchVertRows; i++) {
                var z = halfDepth - i*patchDepth;
                for (int j = 0; j < _numPatchVertCols; j++) {
                    var x = -halfWidth + j*patchWidth;
                    var vertId = i * _numPatchVertCols + j;
                    patchVerts[vertId]= new Vertex.TerrainCP(
                        new Vector3(x, 0, z), 
                        new Vector2(j*du, i*dv), 
                        new Vector2()
                    );
                }
            }
            for (int i = 0; i < _numPatchVertRows-1; i++) {
                for (int j = 0; j < _numPatchVertCols-1; j++) {
                    var patchID = i * (_numPatchVertCols - 1) + j;
                    var vertID = i * _numPatchVertCols + j;
                    patchVerts[vertID].BoundsY = _patchBoundsY[patchID];
                }
            }

            var vbd = new BufferDescription(
                Vertex.TerrainCP.Stride*patchVerts.Length, 
                ResourceUsage.Immutable, 
                BindFlags.VertexBuffer, 
                CpuAccessFlags.None, 
                ResourceOptionFlags.None, 0
            );
            _quadPatchVB = new Buffer(
                device, 
                new DataStream(patchVerts, false, false), 
                vbd
            );
        }
    }

    
}
