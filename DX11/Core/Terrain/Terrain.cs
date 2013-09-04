using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Core.FX;
using SlimDX;
using SlimDX.DXGI;
using SlimDX.Direct3D11;
using Buffer = SlimDX.Direct3D11.Buffer;
using Device = SlimDX.Direct3D11.Device;

namespace Core.Terrain {
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

        private int _numPatchVertRows;
        private int _numPatchVertCols;

        public Matrix World { get; set; }

        private Material _material;

        private List<Vector2> _patchBoundsY;
        private List<float> _heightMap;

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
        public float Depth { get { return (_info.HeightMapHeight - 1)*-_info.CellSpacing; } }
        public float Height(float x, float z) {
            var c = (x + 0.5f*Width)/_info.CellSpacing;
            var d = (z + 0.5f*Depth)/-_info.CellSpacing;
            var row = (int)Math.Floor(d);
            var col = (int) Math.Floor(c);

            var A = _heightMap[row*_info.HeightMapWidth + col];
            var B = _heightMap[row*_info.HeightMapWidth + col + 1];
            var C = _heightMap[(row +1)* _info.HeightMapWidth + col];
            var D = _heightMap[(row +1)* _info.HeightMapWidth + col + 1];

            var s = c - col;
            var t = d - row;

            if (s + t <= 1.0f) {
                var uy = B - A;
                var vy = B - D;
                return A + (1.0f - s)*uy + (1.0f - t)*vy;
            } else {
                var uy = C - D;
                var vy = B - D;
                return D + (1.0f - s)*uy + (1.0f-t)*vy;
            }
        }
        public void Init(Device device, DeviceContext dc, InitInfo info) {
            _info = info;
            _numPatchVertRows = ((_info.HeightMapHeight - 1)/CellsPerPatch) + 1;
            _numPatchVertCols = ((_info.HeightMapWidth - 1)/CellsPerPatch) + 1;
            _numPatchVertices = _numPatchVertRows*_numPatchVertCols;
            _numPatchQuadFaces = (_numPatchVertRows - 1)*(_numPatchVertCols - 1);

            LoadHeightmap();
            Smooth();
            CalcAllPatchBoundsY();

            BuildQuadPatchVB(device);
            BuildQuadPatchIB(device);
            BuildHeightmapSRV(device);

            var layerFilenames = new List<string> {
                _info.LayerMapFilename0,
                _info.LayerMapFilename1,
                _info.LayerMapFilename2,
                _info.LayerMapFilename3,
                _info.LayerMapFilename4
            };
            _layerMapArraySRV = Util.CreateTexture2DArraySRV(device, dc, layerFilenames.ToArray(), Format.R8G8B8A8_UNorm);
            _blendMapSRV = ShaderResourceView.FromFile(device, _info.BlendMapFilename);
        }
        public void Draw(DeviceContext dc, Camera.CameraBase cam, DirectionalLight[] lights) {
            dc.InputAssembler.PrimitiveTopology = PrimitiveTopology.PatchListWith4ControlPoints;
            dc.InputAssembler.InputLayout = InputLayouts.Terrain;

            var stride = Vertex.Terrain.Stride;
            var offset = 0;

            dc.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_quadPatchVB, stride, offset));
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

        private void BuildHeightmapSRV(Device device) {
            var texDec = new Texture2DDescription {
                ArraySize = 1,
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = Format.R16_Float,
                SampleDescription = new SampleDescription(1, 0),
                Height = _info.HeightMapHeight,
                Width = _info.HeightMapWidth,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                Usage = ResourceUsage.Default
            };
            var hmap = Half.ConvertToHalf(_heightMap.ToArray());

            var hmapTex = new Texture2D(device, texDec, new DataRectangle(_info.HeightMapWidth*Marshal.SizeOf(typeof(Half)), new DataStream(hmap.ToArray(), false, false)));

            var srvDesc = new ShaderResourceViewDescription {
                Format = texDec.Format,
                Dimension = ShaderResourceViewDimension.Texture2D,
                MostDetailedMip = 0,
                MipLevels = -1
            };
            _heightMapSRV = new ShaderResourceView(device, hmapTex, srvDesc);
            Util.ReleaseCom(ref hmapTex);
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
            var ibd = new BufferDescription(sizeof (short)*indices.Count, ResourceUsage.Immutable, BindFlags.IndexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _quadPatchIB = new Buffer(device, new DataStream(indices.Select(i=>(short)i).ToArray(), false, false), ibd);
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

            var minY = float.PositiveInfinity;
            var maxY = float.NegativeInfinity;

            for (var y = y0; y < y1; y++) {
                for (var x = x0; x < x1; x++) {
                    var k = y*_info.HeightMapWidth + x;
                    minY = Math.Min(minY, _heightMap[k]);
                    maxY = Math.Max(maxY, _heightMap[k]);
                }
            }
            var patchID = i*(_numPatchVertCols - 1) + j;
            _patchBoundsY[patchID] = new Vector2(minY, maxY);
        }

        private void Smooth() {
            var dest = new List<float>();
            for (var i = 0; i < _info.HeightMapHeight; i++) {
                for (var j = 0; j < _info.HeightMapWidth; j++) {
                    dest.Add(Average(i,j));
                }
            }
        }

        private float Average(int i, int j) {
            var avg = 0.0f;
            var num = 0.0f;
            for (var m = i-1; m <= i+1; m++) {
                for (var n = j-1; n <= j+1; n++) {
                    if (!InBounds(m, n)) continue;

                    avg += _heightMap[m*_info.HeightMapWidth + n];
                    num++;
                }
            }
            return avg/num;
        }

        private bool InBounds(int i, int j) {
            return i >= 0 && i < _info.HeightMapHeight && j >= 0 && j < _info.HeightMapWidth;
        }

        private void LoadHeightmap() {
            var input = File.ReadAllBytes(_info.HeightMapFilename);

            _heightMap = input.Select(i => (i/255.0f*_info.HeightScale)).ToList();
        }
        private void BuildQuadPatchVB(Device device) {
            var patchVerts = new List<Vertex.Terrain>();
            var halfWidth = 0.5f*Width;
            var halfDepth = 0.5f*Depth;

            var patchWidth = Width/(_numPatchVertCols - 1);
            var patchDepth = Depth/(_numPatchVertRows - 1);
            var du = 1.0f/(_numPatchVertCols - 1);
            var dv = 1.0f/(_numPatchVertRows - 1);

            for (int i = 0; i < _numPatchVertRows; i++) {
                var z = -halfDepth - i*patchDepth;
                for (int j = 0; j < _numPatchVertCols; j++) {
                    var x = -halfWidth + j*patchWidth;
                    if (i == _numPatchVertRows - 1 || j == _numPatchVertCols - 1) {
                        patchVerts.Add(new Vertex.Terrain(new Vector3(x, 0, z), new Vector2(j*du, i*dv), new Vector2()));
                    } else {
                        var patchID = i*(_numPatchVertCols - 1) + j;
                        patchVerts.Add(new Vertex.Terrain(new Vector3(x, 0, z), new Vector2(j*du, i*dv), _patchBoundsY[patchID]));
                    }
                }
            }
            var vbd = new BufferDescription(Vertex.Terrain.Stride*patchVerts.Count, ResourceUsage.Immutable, BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _quadPatchVB = new Buffer(device, new DataStream(patchVerts.ToArray(), false, false), vbd);

        }
    }

    public struct InitInfo {
        public string HeightMapFilename;
        public string LayerMapFilename0;
        public string LayerMapFilename1;
        public string LayerMapFilename2;
        public string LayerMapFilename3;
        public string LayerMapFilename4;
        public string BlendMapFilename;
        public float HeightScale;
        public int HeightMapWidth;
        public int HeightMapHeight;
        public float CellSpacing;
    }
}
