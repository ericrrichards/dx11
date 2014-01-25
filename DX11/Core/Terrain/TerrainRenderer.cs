namespace Core.Terrain {
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    using Camera;
    using FX;
    using Vertex;

    using SlimDX;
    using SlimDX.Direct3D11;
    using SlimDX.DXGI;

    using Buffer = SlimDX.Direct3D11.Buffer;
    using Device = SlimDX.Direct3D11.Device;



    public class TerrainRenderer : DisposableClass {
        internal const float MaxDist = 500.0f;
        internal const float MinDist = 20.0f;
        internal const float MaxTess = 6.0f;
        internal const float MinTess = 0.0f;
        private readonly Terrain _terrain;
        private Buffer _quadPatchVB;
        private Buffer _quadPatchIB;
        private readonly List<Patch> _patches;
        private ShaderResourceView _layerMapArraySRV;
        private ShaderResourceView _blendMapSRV;
        private ShaderResourceView _heightMapSRV;
        private int _numPatchVertices;
        private int _numPatchQuadFaces;
        public int NumPatchVertRows;
        public int NumPatchVertCols;
        private Material _material;
        private List<Vector2> _patchBoundsY;
        private bool _useTessellation;
        private readonly List<VertexPC> _bvhVerts = new List<VertexPC>();

        private readonly List<int> _bvhIndices = new List<int>();

        private Buffer _bvhVB;

        private Buffer _bvhIB;

        private int _aabCount;

        private bool _disposed;

        public bool DebugQuadTree { get; set; }

        public bool Shadows { get; set; }
        public Matrix World { get; set; }

        private Device _device;
        private WalkMap _walkMap;

        public TerrainRenderer(Material material, Terrain terrain) {
            _material = material;
            _patches = new List<Patch>();
            _terrain = terrain;
            World = Matrix.Identity;

        }

        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Util.ReleaseCom(ref _quadPatchVB);
                    Util.ReleaseCom(ref _quadPatchIB);

                    Util.ReleaseCom(ref _layerMapArraySRV);
                    Util.ReleaseCom(ref _blendMapSRV);
                    Util.ReleaseCom(ref _heightMapSRV);
                    Util.ReleaseCom(ref _walkMap);

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

        public void Init(Device device, DeviceContext dc, Terrain terrain) {
            _device = device;
            NumPatchVertRows = ((terrain.Info.HeightMapHeight - 1) / Terrain.CellsPerPatch) + 1;
            NumPatchVertCols = ((terrain.Info.HeightMapWidth - 1) / Terrain.CellsPerPatch) + 1;
            _numPatchVertices = NumPatchVertRows * NumPatchVertCols;
            _numPatchQuadFaces = (NumPatchVertRows - 1) * (NumPatchVertCols - 1);
            if (device.FeatureLevel == FeatureLevel.Level_11_0) {
                _useTessellation = true;
            }

            if (terrain.Info.Material.HasValue) {
                _material = terrain.Info.Material.Value;
            }

            D3DApp.GD3DApp.ProgressUpdate.Draw(0.60f, "Building terrain patches");
            if (_useTessellation) {
                CalcAllPatchBoundsY();
                BuildQuadPatchVB(device);
                BuildQuadPatchIB(device);
            } else {
                BuildPatches(device);
            }
            D3DApp.GD3DApp.ProgressUpdate.Draw(0.65f, "Loading textures");
            _heightMapSRV = terrain.HeightMap.BuildHeightmapSRV(device);

            var layerFilenames = new List<string> {
                terrain.Info.LayerMapFilename0 ?? "textures/null.bmp", 
                terrain.Info.LayerMapFilename1 ?? "textures/null.bmp", 
                terrain.Info.LayerMapFilename2 ?? "textures/null.bmp", 
                terrain.Info.LayerMapFilename3 ?? "textures/null.bmp", 
                terrain.Info.LayerMapFilename4 ?? "textures/null.bmp"
            };
            _layerMapArraySRV = Util.CreateTexture2DArraySRV(device, dc, layerFilenames.ToArray(), Format.R8G8B8A8_UNorm);
            if (!string.IsNullOrEmpty(terrain.Info.BlendMapFilename)) {
                D3DApp.GD3DApp.ProgressUpdate.Draw(0.70f, "Loading blendmap from file");
                _blendMapSRV = ShaderResourceView.FromFile(device, terrain.Info.BlendMapFilename);
                _blendMapSRV.Resource.DebugName = terrain.Info.BlendMapFilename;
            } else {
                _blendMapSRV = CreateBlendMap(terrain.HeightMap, device, terrain);
            }

            if (DebugQuadTree) {
                D3DApp.GD3DApp.ProgressUpdate.Draw(0.95f, "Building quadtree debug vertex buffers");
                BuildQuadTreeDebugBuffers(device);
            }
            D3DApp.GD3DApp.ProgressUpdate.Draw(1.0f, "Terrain initialized");
            _walkMap = new WalkMap(this);
        }
        #region DX11 Terrain Mesh Creation
        private void CalcAllPatchBoundsY() {
            _patchBoundsY = new List<Vector2>(new Vector2[_numPatchQuadFaces]);

            for (var i = 0; i < NumPatchVertRows - 1; i++) {
                for (var j = 0; j < NumPatchVertCols - 1; j++) {
                    CalcPatchBoundsY(i, j);
                }
            }
        }

        private void CalcPatchBoundsY(int i, int j) {
            var x0 = j * Terrain.CellsPerPatch;
            var x1 = (j + 1) * Terrain.CellsPerPatch;

            var y0 = i * Terrain.CellsPerPatch;
            var y1 = (i + 1) * Terrain.CellsPerPatch;

            var minY = float.MaxValue;
            var maxY = float.MinValue;

            for (var y = y0; y <= y1; y++) {
                for (var x = x0; x <= x1; x++) {
                    minY = Math.Min(minY, _terrain.HeightMap[y, x]);
                    maxY = Math.Max(maxY, _terrain.HeightMap[y, x]);
                }
            }
            var patchID = i * (NumPatchVertCols - 1) + j;
            _patchBoundsY[patchID] = new Vector2(minY, maxY);
        }

        private void BuildQuadPatchVB(Device device) {
            var patchVerts = new TerrainCP[_numPatchVertices];
            var halfWidth = 0.5f * _terrain.Width;
            var halfDepth = 0.5f * _terrain.Depth;

            var patchWidth = _terrain.Width / (NumPatchVertCols - 1);
            var patchDepth = _terrain.Depth / (NumPatchVertRows - 1);
            var du = 1.0f / (NumPatchVertCols - 1);
            var dv = 1.0f / (NumPatchVertRows - 1);

            for (var i = 0; i < NumPatchVertRows; i++) {
                var z = halfDepth - i * patchDepth;
                for (var j = 0; j < NumPatchVertCols; j++) {
                    var x = -halfWidth + j * patchWidth;
                    var vertId = i * NumPatchVertCols + j;
                    patchVerts[vertId] = new TerrainCP(
                        new Vector3(x, 0, z),
                        new Vector2(j * du, i * dv),
                        new Vector2()
                        );
                }
            }
            for (var i = 0; i < NumPatchVertRows - 1; i++) {
                for (var j = 0; j < NumPatchVertCols - 1; j++) {
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
        #endregion
        #region DX10 Terrain Mesh Creation
        private void BuildPatches(Device device) {
            foreach (var p in _patches) {
                var patch = p;
                Util.ReleaseCom(ref patch);
            }
            _patches.Clear();

            for (var z = 0; z < (NumPatchVertRows - 1); z++) {
                var z1 = z * Terrain.CellsPerPatch;
                for (var x = 0; x < (NumPatchVertCols - 1); x++) {
                    var x1 = x * Terrain.CellsPerPatch;
                    var r = new Rectangle(x1, z1, Terrain.CellsPerPatch, Terrain.CellsPerPatch);
                    var p = new Patch();
                    p.CreateMesh(_terrain, r, device);
                    _patches.Add(p);
                }
                D3DApp.GD3DApp.ProgressUpdate.Draw(0.75f + 0.1f * ((float)z / (NumPatchVertRows - 2)), "Building terrain patches");
            }
        }
        #endregion
        #region BlendMap
        private static ShaderResourceView CreateBlendMap(HeightMap hm, Device device, Terrain terrain) {
            var colors = new List<Color4>();
            for (var y = 0; y < terrain.HeightMap.HeightMapHeight; y++) {
                for (var x = 0; x < terrain.HeightMap.HeightMapWidth; x++) {
                    var elev = terrain.HeightMap[y, x];
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
                D3DApp.GD3DApp.ProgressUpdate.Draw(0.70f + 0.05f * ((float)y / terrain.HeightMap.HeightMapHeight), "Generating blendmap");
            }
            SmoothBlendMap(hm, colors, terrain);
            SmoothBlendMap(hm, colors, terrain);
            var texDec = new Texture2DDescription {
                ArraySize = 1,
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = Format.R32G32B32A32_Float,
                SampleDescription = new SampleDescription(1, 0),
                Height = terrain.HeightMap.HeightMapHeight,
                Width = terrain.HeightMap.HeightMapWidth,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                Usage = ResourceUsage.Default
            };
            var blendTex = new Texture2D(device, texDec, new DataRectangle(terrain.HeightMap.HeightMapWidth * Marshal.SizeOf(typeof(Color4)), new DataStream(colors.ToArray(), false, false))) { DebugName = "terrain blend texture" };
            var srvDesc = new ShaderResourceViewDescription { Format = texDec.Format, Dimension = ShaderResourceViewDimension.Texture2D, MostDetailedMip = 0, MipLevels = -1 };

            var srv = new ShaderResourceView(device, blendTex, srvDesc);

            Util.ReleaseCom(ref blendTex);
            return srv;
        }

        private static void SmoothBlendMap(HeightMap hm, List<Color4> colors, Terrain terrain) {
            for (var y = 0; y < terrain.HeightMap.HeightMapHeight; y++) {
                for (var x = 0; x < terrain.HeightMap.HeightMapWidth; x++) {
                    var sum = colors[x + y * hm.HeightMapHeight];
                    var num = 0;
                    for (var y1 = y - 1; y1 < y + 2; y1++) {
                        for (var x1 = x - 1; x1 < x + 1; x1++) {
                            if (!hm.InBounds(y1, x1)) {
                                continue;
                            }
                            sum += colors[x1 + y1 * hm.HeightMapHeight];
                            num++;
                        }
                    }
                    colors[x + y * hm.HeightMapHeight] = new Color4(sum.Alpha / num, sum.Red / num, sum.Green / num, sum.Blue / num);
                }
            }
        }
        #endregion
        #region Debug QuadTree
        private void BuildQuadTreeDebugBuffers(Device device) {
            GetQuadTreeVerticesAndIndices(_terrain.QuadTree.Root);
            var vbd = new BufferDescription(VertexPC.Stride * _bvhVerts.Count, ResourceUsage.Immutable,
                BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _bvhVB = new Buffer(device, new DataStream(_bvhVerts.ToArray(), false, false), vbd);
            var ibd = new BufferDescription(sizeof(int) * _bvhIndices.Count, ResourceUsage.Immutable,
                BindFlags.IndexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _bvhIB = new Buffer(device, new DataStream(_bvhIndices.ToArray(), false, false), ibd);
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

        private void DrawQuadTreeDebugBuffers(DeviceContext dc, CameraBase cam, int offset) {
            dc.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineList;
            dc.InputAssembler.InputLayout = InputLayouts.PosColor;
            dc.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_bvhVB, VertexPC.Stride, offset));
            dc.InputAssembler.SetIndexBuffer(_bvhIB, Format.R32_UInt, 0);

            //dc.OutputMerger.DepthStencilState = RenderStates.NoDepthDSS;
            Effects.ColorFX.SetWorldViewProj(cam.ViewProj);
            for (var p = 0; p < Effects.ColorFX.ColorTech.Description.PassCount; p++) {
                Effects.ColorFX.ColorTech.GetPassByIndex(p).Apply(dc);
                dc.DrawIndexed(_bvhIndices.Count, 0, 0);
            }
            dc.OutputMerger.DepthStencilState = null;
        }

        #endregion

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
                Effects.TerrainFX.SetTexelCellSpaceU(1.0f / _terrain.Info.HeightMapWidth);
                Effects.TerrainFX.SetTexelCellSpaceV(1.0f / _terrain.Info.HeightMapHeight);
                Effects.TerrainFX.SetWorldCellSpace(_terrain.Info.CellSpacing);
                Effects.TerrainFX.SetWorldFrustumPlanes(planes);
                Effects.TerrainFX.SetLayerMapArray(_layerMapArraySRV);
                Effects.TerrainFX.SetBlendMap(_blendMapSRV);
                Effects.TerrainFX.SetHeightMap(_heightMapSRV);
                Effects.TerrainFX.SetMaterial(_material);
                Effects.TerrainFX.SetViewProjTex(viewProj * toTexSpace);
                Effects.TerrainFX.SetWalkMap(_walkMap.WalkableTiles);
                Effects.TerrainFX.SetUnwalkableTex(_walkMap.UnwalkableSRV);

                var tech = Shadows ? Effects.TerrainFX.Light1ShadowTech : Effects.TerrainFX.Light1Tech;
                for (var p = 0; p < tech.Description.PassCount; p++) {
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

                Effects.TerrainFX.SetTexelCellSpaceU(1.0f / _terrain.Info.HeightMapWidth);
                Effects.TerrainFX.SetTexelCellSpaceV(1.0f / _terrain.Info.HeightMapHeight);
                Effects.TerrainFX.SetWorldCellSpace(_terrain.Info.CellSpacing);

                Effects.TerrainFX.SetLayerMapArray(_layerMapArraySRV);
                Effects.TerrainFX.SetBlendMap(_blendMapSRV);
                Effects.TerrainFX.SetHeightMap(_heightMapSRV);
                Effects.TerrainFX.SetMaterial(_material);
                Effects.TerrainFX.SetWalkMap(_walkMap.WalkableTiles);
                Effects.TerrainFX.SetUnwalkableTex(_walkMap.UnwalkableSRV);
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

        public void ComputeSsao(DeviceContext dc, CameraBase cam, Ssao ssao, DepthStencilView depthStencilView) {
            ssao.SetNormalDepthRenderTarget(depthStencilView);

            Effects.SsaoFX.SetOcclusionRadius(0.1f);
            Effects.SsaoFX.SetOcclusionFadeEnd(0.75f);

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
            Effects.TerrainFX.SetTexelCellSpaceU(1.0f / _terrain.Info.HeightMapWidth);
            Effects.TerrainFX.SetTexelCellSpaceV(1.0f / _terrain.Info.HeightMapHeight);
            Effects.TerrainFX.SetWorldCellSpace(_terrain.Info.CellSpacing);
            Effects.TerrainFX.SetWorldFrustumPlanes(planes);
            Effects.TerrainFX.SetHeightMap(_heightMapSRV);
            Effects.TerrainFX.SetView(cam.View);

            var tech = Effects.TerrainFX.NormalDepthTech;
            for (var p = 0; p < tech.Description.PassCount; p++) {
                var pass = tech.GetPassByIndex(p);
                pass.Apply(dc);
                dc.DrawIndexed(_numPatchQuadFaces * 4, 0, 0);
            }
            dc.HullShader.Set(null);
            dc.DomainShader.Set(null);

            ssao.ComputeSsao(cam);
            ssao.BlurAmbientMap(4);
            Effects.SsaoFX.SetOcclusionRadius(0.5f);
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
            Effects.TerrainFX.SetWorldCellSpace(_terrain.Info.CellSpacing);
            Effects.TerrainFX.SetWorldFrustumPlanes(planes);
            Effects.TerrainFX.SetHeightMap(_heightMapSRV);
            

            var tech = Effects.TerrainFX.TessBuildShadowMapTech;
            for (var p = 0; p < tech.Description.PassCount; p++) {
                var pass = tech.GetPassByIndex(p);
                pass.Apply(dc);
                dc.DrawIndexed(_numPatchQuadFaces * 4, 0, 0);
            }
            dc.HullShader.Set(null);
            dc.DomainShader.Set(null);
        }

        private class WalkMap : DisposableClass {
            private bool _disposed;
            private readonly TerrainRenderer _terrainRenderer;
            internal ShaderResourceView WalkableTiles;
            internal ShaderResourceView UnwalkableSRV;

            public WalkMap(TerrainRenderer terrainRenderer) {
                _terrainRenderer = terrainRenderer;
                UnwalkableSRV = ShaderResourceView.FromFile(terrainRenderer._device, "textures/unwalkable.png");
                CreateWalkableTexture(
                    terrainRenderer._terrain.Tiles,
                    terrainRenderer._terrain.WidthInTiles,
                    terrainRenderer._terrain.HeightInTiles
                );
            }

            protected override void Dispose(bool disposing) {
                if (!_disposed) {
                    if (disposing) {
                        Util.ReleaseCom(ref WalkableTiles);
                        Util.ReleaseCom(ref UnwalkableSRV);
                    }
                    _disposed = true;
                }
                base.Dispose(disposing);
            }

            private void CreateWalkableTexture(IList<MapTile> tiles, int widthInTiles, int heightInTiles) {
                // create the texture description for the walkable map
                // it should have the same dimensions as the tile map
                var desc = new Texture2DDescription {
                    ArraySize = 1,
                    BindFlags = BindFlags.ShaderResource,
                    CpuAccessFlags = CpuAccessFlags.None,
                    Format = Format.R8_UNorm,
                    Height = heightInTiles,
                    Width = widthInTiles,
                    MipLevels = 1,
                    OptionFlags = ResourceOptionFlags.None,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Default
                };

                // create the pixel data
                var colors = new List<byte>();
                for (var y = 0; y < heightInTiles; y++) {
                    for (var x = 0; x < widthInTiles; x++) {
                        // walkable tiles are black, unwalkable tiles are white
                        colors.Add((byte)(tiles[x + widthInTiles * y].Walkable ? 0 : 255));
                    }
                }
                // do a bilinear smoothing on the walkable map, to smooth the transition between the normal and unwalkable textures
                for (var y = 0; y < heightInTiles; y++) {
                    for (var x = 0; x < widthInTiles; x++) {
                        float temp = 0;
                        var num = 0;
                        for (var y1 = y - 1; y1 <= y + 1; y1++) {
                            for (var x1 = x - 1; x1 <= x + 1; x1++) {
                                if (y1 < 0 || y1 >= heightInTiles || x1 < 0 || x1 >= widthInTiles) {
                                    continue;
                                }
                                temp += colors[x1 + y1 * widthInTiles];
                                num++;
                            }
                        }
                        colors[x + y * widthInTiles] = (byte)(temp / num);
                    }
                }

                // create the texture from the pixel data and create the ShaderResourceView
                var walkMap = new Texture2D(
                    _terrainRenderer._device,
                    desc,
                    new DataRectangle(widthInTiles * sizeof(byte), new DataStream(colors.ToArray(), false, false)));
                WalkableTiles = new ShaderResourceView(_terrainRenderer._device, walkMap);

                Util.ReleaseCom(ref walkMap);
            }
        }
    }
}