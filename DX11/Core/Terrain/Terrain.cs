using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Core.FX;
using Core.Vertex;
using SlimDX;
using SlimDX.DXGI;
using SlimDX.Direct3D11;
using Buffer = SlimDX.Direct3D11.Buffer;
using Device = SlimDX.Direct3D11.Device;

namespace Core.Terrain {
    using System.Runtime.InteropServices;

    using Camera;


    public class Terrain : DisposableClass {
        public const int CellsPerPatch = 64;
        private Buffer _quadPatchVB;
        private Buffer _quadPatchIB;

        private readonly List<Patch> _patches;

        private ShaderResourceView _layerMapArraySRV;
        private ShaderResourceView _blendMapSRV;
        private ShaderResourceView _heightMapSRV;

        internal InitInfo Info;
        private int _numPatchVertices;
        private int _numPatchQuadFaces;

        // number of rows of patch control point vertices
        public int NumPatchVertRows;
        // number of columns of patch control point vertices
        public int NumPatchVertCols;

        public Matrix World { get; set; }

        private Material _material;

        // computed Y bounds for each patch
        private List<Vector2> _patchBoundsY;
        private HeightMap _heightMap;

        private bool _disposed;
        private bool _useTessellation;
        internal const float MaxDist = 500.0f;
        internal const float MinDist = 20.0f;
        internal const float MaxTess = 6.0f;
        internal const float MinTess = 0.0f;

        public Image HeightMapImg { get { return _heightMap.Bitmap; } }

        private QuadTree _quadTree;

        private readonly List<VertexPC> _bvhVerts = new List<VertexPC>();
        private readonly List<int> _bvhIndices = new List<int>();
        private Buffer _bvhVB;
        private Buffer _bvhIB;

        private int _aabCount;
        public bool DebugQuadTree { get; set; }
        public bool Shadows { get; set; }

        public Terrain() {
            World = Matrix.Identity;
            _material = new Material {
                Ambient = Color.White,
                Diffuse = Color.White,
                Specular = new Color4(64.0f, 0, 0, 0),
                Reflect = Color.Black
            };

            _patches = new List<Patch>();
        }
        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Util.ReleaseCom(ref _quadPatchVB);
                    Util.ReleaseCom(ref _quadPatchIB);

                    Util.ReleaseCom(ref _layerMapArraySRV);
                    Util.ReleaseCom(ref _blendMapSRV);
                    Util.ReleaseCom(ref _heightMapSRV);

                    foreach (var p in _patches) {
                        var patch = p;
                        Util.ReleaseCom(ref patch);
                    }
                    _patches.Clear();
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }
        public float Width { get { return (Info.HeightMapWidth - 1) * Info.CellSpacing; } }
        public float Depth { get { return (Info.HeightMapHeight - 1) * Info.CellSpacing; } }
        public float Height(float x, float z) {
            var c = (x + 0.5f * Width) / Info.CellSpacing;
            var d = (z - 0.5f * Depth) / -Info.CellSpacing;
            var row = (int)Math.Floor(d);
            var col = (int)Math.Floor(c);

            var h00 = _heightMap[row, col];
            var h01 = _heightMap[row, col + 1];
            var h10 = _heightMap[(row + 1), col];
            var h11 = _heightMap[(row + 1), col + 1];

            var s = c - col;
            var t = d - row;

            if (s + t <= 1.0f) {
                var uy = h01 - h00;
                var vy = h01 - h11;
                return h00 + (1.0f - s) * uy + (1.0f - t) * vy;
            } else {
                var uy = h10 - h11;
                var vy = h01 - h11;
                return h11 + (1.0f - s) * uy + (1.0f - t) * vy;
            }
        }
        public void Init(Device device, DeviceContext dc, InitInfo info) {
            D3DApp.GD3DApp.ProgressUpdate.Draw(0, "Initializing terrain");
            if (device.FeatureLevel == FeatureLevel.Level_11_0) {
                _useTessellation = true;
            }
            Info = info;
            NumPatchVertRows = ((Info.HeightMapHeight - 1) / CellsPerPatch) + 1;
            NumPatchVertCols = ((Info.HeightMapWidth - 1) / CellsPerPatch) + 1;
            _numPatchVertices = NumPatchVertRows * NumPatchVertCols;
            _numPatchQuadFaces = (NumPatchVertRows - 1) * (NumPatchVertCols - 1);

            if (Info.Material.HasValue) {
                _material = Info.Material.Value;
            }

            _heightMap = new HeightMap(Info.HeightMapWidth, Info.HeightMapHeight, Info.HeightScale);
            if (!string.IsNullOrEmpty(Info.HeightMapFilename)) {
                D3DApp.GD3DApp.ProgressUpdate.Draw(0.1f, "Loading terrain from file");
                _heightMap.LoadHeightmap(Info.HeightMapFilename);

            } else {
                D3DApp.GD3DApp.ProgressUpdate.Draw(0.1f, "Generating random terrain");
                GenerateRandomTerrain();
                D3DApp.GD3DApp.ProgressUpdate.Draw(0.50f, "Smoothing terrain");
                _heightMap.Smooth(true);
            }


            D3DApp.GD3DApp.ProgressUpdate.Draw(0.75f, "Building terrain patches");
            if (_useTessellation) {
                CalcAllPatchBoundsY();
                BuildQuadPatchVB(device);
                BuildQuadPatchIB(device);
            } else {
                BuildPatches(device);
            }
            D3DApp.GD3DApp.ProgressUpdate.Draw(0.85f, "Loading textures");
            _heightMapSRV = _heightMap.BuildHeightmapSRV(device);

            var layerFilenames = new List<string> {
                Info.LayerMapFilename0 ?? "textures/null.bmp",
                Info.LayerMapFilename1 ?? "textures/null.bmp",
                Info.LayerMapFilename2 ?? "textures/null.bmp",
                Info.LayerMapFilename3 ?? "textures/null.bmp",
                Info.LayerMapFilename4 ?? "textures/null.bmp"
            };
            _layerMapArraySRV = Util.CreateTexture2DArraySRV(device, dc, layerFilenames.ToArray(), Format.R8G8B8A8_UNorm);
            if (!string.IsNullOrEmpty(Info.BlendMapFilename)) {
                D3DApp.GD3DApp.ProgressUpdate.Draw(0.95f, "Loading blendmap from file");
                _blendMapSRV = ShaderResourceView.FromFile(device, Info.BlendMapFilename);
                _blendMapSRV.Resource.DebugName = Info.BlendMapFilename;
            } else {
                _blendMapSRV = CreateBlendMap(_heightMap, device);
            }
            D3DApp.GD3DApp.ProgressUpdate.Draw(0.97f, "Building picking quadtree...");
            _quadTree = new QuadTree {
                Root = BuildQuadTree(
                    new Vector2(0, 0),
                    new Vector2((Info.HeightMapWidth - 1), (Info.HeightMapHeight - 1))
                )
            };
            if (DebugQuadTree) {
                D3DApp.GD3DApp.ProgressUpdate.Draw(0.99f, "Building quadtree debug vertex buffers");
                BuildQuadTreeDebugBuffers(device);
            }
            D3DApp.GD3DApp.ProgressUpdate.Draw(1.0f, "Terrain initialized");
        }

        private void BuildQuadTreeDebugBuffers(Device device) {
            GetQuadTreeVerticesAndIndices(_quadTree.Root);
            var vbd = new BufferDescription(VertexPC.Stride * _bvhVerts.Count, ResourceUsage.Immutable,
                BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _bvhVB = new Buffer(device, new DataStream(_bvhVerts.ToArray(), false, false), vbd);
            var ibd = new BufferDescription(sizeof(int) * _bvhIndices.Count, ResourceUsage.Immutable,
                BindFlags.IndexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _bvhIB = new Buffer(device, new DataStream(_bvhIndices.ToArray(), false, false), ibd);
        }

        private QuadTreeNode BuildQuadTree(Vector2 topLeft, Vector2 bottomRight) {

            const float tolerance = 0.01f;

            // search the heightmap in order to get the y-extents of the terrain region
            var minMaxY = GetMinMaxY(topLeft, bottomRight);

            // convert the heightmap index bounds into world-space coordinates
            var minX = topLeft.X * Info.CellSpacing - Width / 2;
            var maxX = bottomRight.X * Info.CellSpacing - Width / 2;
            var minZ = -topLeft.Y * Info.CellSpacing + Depth / 2;
            var maxZ = -bottomRight.Y*Info.CellSpacing + Depth/2;

            // adjust the bounds to get a very slight overlap of the bounding boxes
            minX -= tolerance;
            maxX += tolerance;
            minZ += tolerance;
            maxZ -= tolerance;

            // construct the new node and assign the world-space bounds of the terrain region
            var quadNode = new QuadTreeNode {
                Bounds = new BoundingBox(
                    new Vector3(minX, minMaxY.X, minZ),
                    new Vector3(maxX, minMaxY.Y, maxZ)
                )
            };

            var width = (int)Math.Floor((bottomRight.X - topLeft.X) / 2);
            var depth = (int)Math.Floor((bottomRight.Y - topLeft.Y) / 2);

            // we will recurse until the terrain regions match our logical terrain tile sizes
            const int tileSize = 2;
            if (width >= tileSize && depth >= tileSize) {
                quadNode.Children = new[] {
                    BuildQuadTree(
                        topLeft, 
                        new Vector2(topLeft.X + width, topLeft.Y + depth)
                    ),
                    BuildQuadTree(
                        new Vector2(topLeft.X + width, topLeft.Y), 
                        new Vector2(bottomRight.X, topLeft.Y + depth) 
                    ),
                    BuildQuadTree(
                        new Vector2(topLeft.X, topLeft.Y+depth), 
                        new Vector2(topLeft.X+depth, bottomRight.Y) 
                    ),
                    BuildQuadTree(
                        new Vector2(topLeft.X+width, topLeft.Y+depth), 
                        bottomRight 
                    )
                };
            }

            return quadNode;
        }



        private Vector2 GetMinMaxY(Vector2 tl, Vector2 br) {
            var max = float.MinValue;
            var min = float.MaxValue;
            for (var x = (int)tl.X; x < br.X; x++) {
                for (var y = (int)tl.Y; y < br.Y; y++) {
                    min = Math.Min(min, _heightMap[y, x]);
                    max = Math.Max(max, _heightMap[y, x]);
                }
            }
            return new Vector2(min, max);
        }

        private void GetQuadTreeVerticesAndIndices(QuadTreeNode quadTree, int level = 0) {
            var vertBase = _bvhVerts.Count;
            var corners = quadTree.Bounds.GetCorners();
            if (level == 9) {
                _bvhVerts.AddRange(corners.Select(c => {
                    var color = Color.White;
                    switch (_aabCount % 4) {
                        case 1:
                            color = Color.Blue;
                            break;
                        case 2:
                            color = Color.Magenta;
                            break;
                        case 3:
                            color = Color.Yellow;
                            break;
                    }
                    return new VertexPC(c, color);
                }));
                _aabCount++;
                _bvhIndices.AddRange(
                    new[] { 0, 1, 1, 2, 2, 3, 3, 0, 4, 5, 5, 6, 6, 7, 7, 4, 4, 0, 5, 1, 7, 3, 6, 2 }.Select(
                        i => i + vertBase));

            }
            if (quadTree.Children != null) {
                foreach (var child in quadTree.Children) {
                    GetQuadTreeVerticesAndIndices(child, level + 1);
                }
            }


        }

        private void GenerateRandomTerrain() {
            var hm2 = new HeightMap(Info.HeightMapWidth, Info.HeightMapHeight, 2.0f);
            _heightMap.CreateRandomHeightMapParallel(Info.Seed, Info.NoiseSize1, Info.Persistence1, Info.Octaves1, true);
            hm2.CreateRandomHeightMapParallel(Info.Seed, Info.NoiseSize2, Info.Persistence2, Info.Octaves2, true);
            hm2.Cap(hm2.MaxHeight * 0.4f);
            _heightMap *= hm2;
        }
        private ShaderResourceView CreateBlendMap(HeightMap hm, Device device) {

            var colors = new List<Color4>();
            for (int y = 0; y < _heightMap.HeightMapHeight; y++) {
                for (int x = 0; x < _heightMap.HeightMapWidth; x++) {
                    var elev = _heightMap[y, x];
                    var color = new Color4(0);
                    if (elev > hm.MaxHeight * (0.05f + MathF.Rand(-0.05f, 0.05f))) {
                        // dark green grass texture
                        color.Red = elev / (hm.MaxHeight) + MathF.Rand(-0.05f, 0.05f);
                    }
                    if (elev > hm.MaxHeight * (0.4f + MathF.Rand(-0.15f, 0.15f))) {
                        // stone texture
                        color.Green = elev / hm.MaxHeight + MathF.Rand(-0.05f, 0.05f);
                    }
                    if (elev > hm.MaxHeight * (0.75f + MathF.Rand(-0.1f, 0.1f))) {
                        // snow texture
                        color.Alpha = elev / hm.MaxHeight + MathF.Rand(-0.05f, 0.05f);
                    }
                    colors.Add(color);

                }
                D3DApp.GD3DApp.ProgressUpdate.Draw(0.95f + 0.05f * ((float)y / _heightMap.HeightMapHeight), "Generating blendmap");
            }
            SmoothBlendMap(hm, colors);
            SmoothBlendMap(hm, colors);
            var texDec = new Texture2DDescription {
                ArraySize = 1,
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = Format.R32G32B32A32_Float,
                SampleDescription = new SampleDescription(1, 0),
                Height = _heightMap.HeightMapHeight,
                Width = _heightMap.HeightMapWidth,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                Usage = ResourceUsage.Default
            };
            var blendTex = new Texture2D(
                device,
                texDec,
                new DataRectangle(
                    _heightMap.HeightMapWidth * Marshal.SizeOf(typeof(Color4)),
                    new DataStream(colors.ToArray(), false, false)
                )
            ) { DebugName = "terrain blend texture" };
            var srvDesc = new ShaderResourceViewDescription {
                Format = texDec.Format,
                Dimension = ShaderResourceViewDimension.Texture2D,
                MostDetailedMip = 0,
                MipLevels = -1
            };

            var srv = new ShaderResourceView(device, blendTex, srvDesc);

            Util.ReleaseCom(ref blendTex);
            return srv;
        }
        private void SmoothBlendMap(HeightMap hm, List<Color4> colors) {
            for (int y = 0; y < _heightMap.HeightMapHeight; y++) {
                for (int x = 0; x < _heightMap.HeightMapWidth; x++) {
                    var sum = colors[x + y * hm.HeightMapHeight];
                    var num = 0;
                    for (int y1 = y - 1; y1 < y + 2; y1++) {
                        for (int x1 = x - 1; x1 < x + 1; x1++) {
                            if (hm.InBounds(y1, x1)) {
                                sum += colors[x1 + y1 * hm.HeightMapHeight];
                                num++;
                            }
                        }
                    }
                    colors[x + y * hm.HeightMapHeight] = new Color4(sum.Alpha / num, sum.Red / num, sum.Green / num, sum.Blue / num);
                }
            }
        }
        public void ComputeSsao(DeviceContext dc, CameraBase cam, Ssao ssao, DepthStencilView depthStencilView) {
            ssao.SetNormalDepthRenderTarget(depthStencilView);

            dc.InputAssembler.PrimitiveTopology = PrimitiveTopology.PatchListWith4ControlPoints;
            dc.InputAssembler.InputLayout = InputLayouts.TerrainCP;

            var stride = TerrainCP.Stride;
            const int offset = 0;

            dc.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_quadPatchVB, stride, offset));
            dc.InputAssembler.SetIndexBuffer(_quadPatchIB, Format.R16_UInt, 0);

            var viewProj = cam.ViewProj;
            var planes = cam.FrustumPlanes;


            Effects.TerrainFX.SetViewProj(viewProj);
            Effects.TerrainFX.SetEyePosW(cam.Position);
            Effects.TerrainFX.SetMinDist(MinDist);
            Effects.TerrainFX.SetMaxDist(MaxDist);
            Effects.TerrainFX.SetMinTess(MinTess);
            Effects.TerrainFX.SetMaxTess(MaxTess);
            Effects.TerrainFX.SetTexelCellSpaceU(1.0f / Info.HeightMapWidth);
            Effects.TerrainFX.SetTexelCellSpaceV(1.0f / Info.HeightMapHeight);
            Effects.TerrainFX.SetWorldCellSpace(Info.CellSpacing);
            Effects.TerrainFX.SetWorldFrustumPlanes(planes);
            Effects.TerrainFX.SetHeightMap(_heightMapSRV);
            Effects.TerrainFX.SetView(cam.View);

            var tech = Effects.TerrainFX.NormalDepthTech;
            for (int p = 0; p < tech.Description.PassCount; p++) {
                var pass = tech.GetPassByIndex(p);
                pass.Apply(dc);
                dc.DrawIndexed(_numPatchQuadFaces * 4, 0, 0);
            }
            dc.HullShader.Set(null);
            dc.DomainShader.Set(null);

            ssao.ComputeSsao(cam);
            ssao.BlurAmbientMap(4);
        }


        public void DrawToShadowMap(DeviceContext dc, ShadowMap sMap, Matrix viewProj) {
            sMap.BindDsvAndSetNullRenderTarget(dc);

            dc.InputAssembler.PrimitiveTopology = PrimitiveTopology.PatchListWith4ControlPoints;
            dc.InputAssembler.InputLayout = InputLayouts.TerrainCP;

            var stride = TerrainCP.Stride;
            const int offset = 0;

            dc.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_quadPatchVB, stride, offset));
            dc.InputAssembler.SetIndexBuffer(_quadPatchIB, Format.R16_UInt, 0);


            var frustum = Frustum.FromViewProj(viewProj);
            var planes = frustum.Planes;

            Effects.TerrainFX.SetViewProj(viewProj);
            Effects.TerrainFX.SetEyePosW(new Vector3(viewProj.M41, viewProj.M42, viewProj.M43));
            Effects.TerrainFX.SetMinDist(MinDist);
            Effects.TerrainFX.SetMaxDist(MaxDist);
            Effects.TerrainFX.SetMinTess(MinTess);
            Effects.TerrainFX.SetMaxTess(MaxTess);
            Effects.TerrainFX.SetWorldCellSpace(Info.CellSpacing);
            Effects.TerrainFX.SetWorldFrustumPlanes(planes);
            Effects.TerrainFX.SetHeightMap(_heightMapSRV);

            var tech = Effects.TerrainFX.TessBuildShadowMapTech;
            for (int p = 0; p < tech.Description.PassCount; p++) {
                var pass = tech.GetPassByIndex(p);
                pass.Apply(dc);
                dc.DrawIndexed(_numPatchQuadFaces * 4, 0, 0);
            }
            dc.HullShader.Set(null);
            dc.DomainShader.Set(null);
        }

        public void Draw(DeviceContext dc, CameraBase cam, DirectionalLight[] lights) {
            if (_useTessellation) {

                dc.InputAssembler.PrimitiveTopology = PrimitiveTopology.PatchListWith4ControlPoints;
                dc.InputAssembler.InputLayout = InputLayouts.TerrainCP;

                var stride = TerrainCP.Stride;
                const int offset = 0;

                dc.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_quadPatchVB, stride, offset));
                dc.InputAssembler.SetIndexBuffer(_quadPatchIB, Format.R16_UInt, 0);

                var viewProj = cam.ViewProj;
                var planes = cam.FrustumPlanes;
                var toTexSpace = Matrix.Scaling(0.5f, -0.5f, 1.0f) * Matrix.Translation(0.5f, 0.5f, 0);

                Effects.TerrainFX.SetViewProj(viewProj);
                Effects.TerrainFX.SetEyePosW(cam.Position);
                Effects.TerrainFX.SetDirLights(lights);
                Effects.TerrainFX.SetFogColor(Color.Silver);
                Effects.TerrainFX.SetFogStart(15.0f);
                Effects.TerrainFX.SetFogRange(175.0f);
                Effects.TerrainFX.SetMinDist(MinDist);
                Effects.TerrainFX.SetMaxDist(MaxDist);
                Effects.TerrainFX.SetMinTess(MinTess);
                Effects.TerrainFX.SetMaxTess(MaxTess);
                Effects.TerrainFX.SetTexelCellSpaceU(1.0f / Info.HeightMapWidth);
                Effects.TerrainFX.SetTexelCellSpaceV(1.0f / Info.HeightMapHeight);
                Effects.TerrainFX.SetWorldCellSpace(Info.CellSpacing);
                Effects.TerrainFX.SetWorldFrustumPlanes(planes);
                Effects.TerrainFX.SetLayerMapArray(_layerMapArraySRV);
                Effects.TerrainFX.SetBlendMap(_blendMapSRV);
                Effects.TerrainFX.SetHeightMap(_heightMapSRV);
                Effects.TerrainFX.SetMaterial(_material);
                Effects.TerrainFX.SetViewProjTex(viewProj * toTexSpace);

                var tech = Shadows ? Effects.TerrainFX.Light1ShadowTech : Effects.TerrainFX.Light1Tech;
                for (int p = 0; p < tech.Description.PassCount; p++) {
                    var pass = tech.GetPassByIndex(p);
                    pass.Apply(dc);
                    dc.DrawIndexed(_numPatchQuadFaces * 4, 0, 0);
                }
                dc.HullShader.Set(null);
                dc.DomainShader.Set(null);

                if (DebugQuadTree) {
                    DrawQuadTreeDebugBuffers(dc, cam, offset);
                }
            } else {
                dc.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
                dc.InputAssembler.InputLayout = InputLayouts.TerrainCP;


                var viewProj = cam.ViewProj;
                Effects.TerrainFX.SetViewProj(viewProj);
                Effects.TerrainFX.SetEyePosW(cam.Position);
                Effects.TerrainFX.SetDirLights(lights);
                Effects.TerrainFX.SetFogColor(Color.Silver);
                Effects.TerrainFX.SetFogStart(15.0f);
                Effects.TerrainFX.SetFogRange(175.0f);

                Effects.TerrainFX.SetTexelCellSpaceU(1.0f / Info.HeightMapWidth);
                Effects.TerrainFX.SetTexelCellSpaceV(1.0f / Info.HeightMapHeight);
                Effects.TerrainFX.SetWorldCellSpace(Info.CellSpacing);

                Effects.TerrainFX.SetLayerMapArray(_layerMapArraySRV);
                Effects.TerrainFX.SetBlendMap(_blendMapSRV);
                Effects.TerrainFX.SetHeightMap(_heightMapSRV);
                Effects.TerrainFX.SetMaterial(_material);
                var tech = Effects.TerrainFX.Light1TechNT;
                for (var p = 0; p < tech.Description.PassCount; p++) {
                    var pass = tech.GetPassByIndex(p);
                    pass.Apply(dc);
                    for (var i = 0; i < _patches.Count; i++) {
                        var patch = _patches[i];
                        if (cam.Visible(patch.Bounds)) {
                            var ns = new Dictionary<NeighborDir, Patch>();
                            if (i < NumPatchVertCols) {
                                ns[NeighborDir.Top] = null;
                            } else {
                                ns[NeighborDir.Top] = _patches[i - NumPatchVertCols + 1];
                            }
                            if (i % (NumPatchVertCols - 1) == 0) {
                                ns[NeighborDir.Left] = null;
                            } else {
                                ns[NeighborDir.Left] = _patches[i - 1];
                            }
                            if (Util.IsKeyDown(Keys.N)) {
                                patch.Draw(dc, cam.Position);
                            } else {
                                patch.Draw(dc, cam.Position, ns);
                            }
                        }
                    }
                }
                if (DebugQuadTree) {
                    DrawQuadTreeDebugBuffers(dc, cam, 0);
                }
            }

        }

        private void DrawQuadTreeDebugBuffers(DeviceContext dc, CameraBase cam, int offset) {
            dc.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineList;
            dc.InputAssembler.InputLayout = InputLayouts.PosColor;
            dc.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_bvhVB, VertexPC.Stride, offset));
            dc.InputAssembler.SetIndexBuffer(_bvhIB, Format.R32_UInt, 0);

            //dc.OutputMerger.DepthStencilState = RenderStates.NoDepthDSS;
            Effects.ColorFX.SetWorldViewProj(cam.ViewProj);
            for (int p = 0; p < Effects.ColorFX.ColorTech.Description.PassCount; p++) {
                Effects.ColorFX.ColorTech.GetPassByIndex(p).Apply(dc);
                dc.DrawIndexed(_bvhIndices.Count, 0, 0);
            }
            dc.OutputMerger.DepthStencilState = null;
        }

        private void BuildQuadPatchIB(Device device) {
            var indices = new List<int>();
            for (var i = 0; i < NumPatchVertRows - 1; i++) {
                for (var j = 0; j < NumPatchVertCols - 1; j++) {
                    indices.Add(i * NumPatchVertCols + j);
                    indices.Add(i * NumPatchVertCols + j + 1);
                    indices.Add((i + 1) * NumPatchVertCols + j);
                    indices.Add((i + 1) * NumPatchVertCols + j + 1);
                }
            }
            var ibd = new BufferDescription(
                sizeof(short) * indices.Count,
                ResourceUsage.Immutable,
                BindFlags.IndexBuffer,
                CpuAccessFlags.None,
                ResourceOptionFlags.None, 0
            );
            _quadPatchIB = new Buffer(
                device,
                new DataStream(indices.Select(i => (short)i).ToArray(), false, false),
                ibd
            );
        }
        private void CalcAllPatchBoundsY() {
            _patchBoundsY = new List<Vector2>(new Vector2[_numPatchQuadFaces]);

            for (var i = 0; i < NumPatchVertRows - 1; i++) {
                for (var j = 0; j < NumPatchVertCols - 1; j++) {
                    CalcPatchBoundsY(i, j);
                }
            }
        }
        private void CalcPatchBoundsY(int i, int j) {
            var x0 = j * CellsPerPatch;
            var x1 = (j + 1) * CellsPerPatch;

            var y0 = i * CellsPerPatch;
            var y1 = (i + 1) * CellsPerPatch;

            var minY = float.MaxValue;
            var maxY = float.MinValue;

            for (var y = y0; y <= y1; y++) {
                for (var x = x0; x <= x1; x++) {
                    minY = Math.Min(minY, _heightMap[y, x]);
                    maxY = Math.Max(maxY, _heightMap[y, x]);
                }
            }
            var patchID = i * (NumPatchVertCols - 1) + j;
            _patchBoundsY[patchID] = new Vector2(minY, maxY);
        }
        private void BuildQuadPatchVB(Device device) {
            var patchVerts = new TerrainCP[_numPatchVertices];
            var halfWidth = 0.5f * Width;
            var halfDepth = 0.5f * Depth;

            var patchWidth = Width / (NumPatchVertCols - 1);
            var patchDepth = Depth / (NumPatchVertRows - 1);
            var du = 1.0f / (NumPatchVertCols - 1);
            var dv = 1.0f / (NumPatchVertRows - 1);

            for (int i = 0; i < NumPatchVertRows; i++) {
                var z = halfDepth - i * patchDepth;
                for (int j = 0; j < NumPatchVertCols; j++) {
                    var x = -halfWidth + j * patchWidth;
                    var vertId = i * NumPatchVertCols + j;
                    patchVerts[vertId] = new TerrainCP(
                        new Vector3(x, 0, z),
                        new Vector2(j * du, i * dv),
                        new Vector2()
                    );
                }
            }
            for (int i = 0; i < NumPatchVertRows - 1; i++) {
                for (int j = 0; j < NumPatchVertCols - 1; j++) {
                    var patchID = i * (NumPatchVertCols - 1) + j;
                    var vertID = i * NumPatchVertCols + j;
                    patchVerts[vertID].BoundsY = _patchBoundsY[patchID];
                }
            }

            var vbd = new BufferDescription(
                TerrainCP.Stride * patchVerts.Length,
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
        private void BuildPatches(Device device) {
            foreach (var p in _patches) {
                var patch = p;
                Util.ReleaseCom(ref patch);
            }
            _patches.Clear();

            for (var z = 0; z < (NumPatchVertRows - 1); z++) {
                var z1 = z * CellsPerPatch;
                for (var x = 0; x < (NumPatchVertCols - 1); x++) {
                    var x1 = x * CellsPerPatch;
                    var r = new Rectangle(x1, z1, CellsPerPatch, CellsPerPatch);
                    var p = new Patch();
                    p.CreateMesh(this, r, device);
                    _patches.Add(p);
                }
                D3DApp.GD3DApp.ProgressUpdate.Draw(0.75f + 0.1f * ((float)z / (NumPatchVertRows - 2)), "Building terrain patches");
            }
        }


        public bool Intersect(Ray ray, ref Vector3 spherePos) {
            Vector3 ret;
            if (!_quadTree.Intersects(ray, out ret)) {
                return false;
            }
            ret.Y = Height(ret.X, ret.Z);
            spherePos = ret;
            return true;
        }
    }
}
