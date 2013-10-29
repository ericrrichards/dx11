using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicCubeMap {
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Windows.Forms;

    using Core;
    using Core.Camera;
    using Core.FX;
    using Core.Vertex;

    using SlimDX;
    using SlimDX.DXGI;
    using SlimDX.Direct3D11;

    internal class DynamicCubeMapDemo : D3DApp {
        private Sky _sky;

        private Buffer _shapesVB;
        private Buffer _shapesIB;

        private Buffer _skullVB;
        private Buffer _skullIB;

        private ShaderResourceView _floorTexSRV;
        private ShaderResourceView _stoneTexSRV;
        private ShaderResourceView _brickTexSRV;

        private DepthStencilView _dynamicCubeMapDSV;
        RenderTargetView[] _dynamicCubeMapRTV = new RenderTargetView[6];
        private ShaderResourceView _dynamicCubeMapSRV;
        private Viewport _cubeMapViewPort;

        private const int CubeMapSize = 256;

        private readonly DirectionalLight[] _dirLights;
        private readonly Material _gridMat;
        private readonly Material _boxMat;
        private readonly Material _cylinderMat;
        private readonly Material _sphereMat;
        private readonly Material _skullMat;
        private readonly Material _centerSphereMat;

        private readonly Matrix[] _sphereWorld = new Matrix[10];
        private readonly Matrix[] _cylWorld = new Matrix[10];
        private readonly Matrix _boxWorld;
        private readonly Matrix _gridWorld;
        private Matrix _skullWorld;
        private readonly Matrix _centerSphereWorld;

        private int _boxVertexOffset;
        private int _gridVertexOffset;
        private int _sphereVertexOffset;
        private int _cylinderVertexOffset;

        private int _boxIndexOffset;
        private int _gridIndexOffset;
        private int _sphereIndexOffset;
        private int _cylinderIndexOffset;

        private int _boxIndexCount;
        private int _gridIndexCount;
        private int _sphereIndexCount;
        private int _cylinderIndexCount;
        private int _skullIndexCount;

        private int _lightCount;
        private FpsCamera _camera;
        private FpsCamera[] _cubeMapCamera = new FpsCamera[6];
        private Point _lastMousePos;


        private bool _disposed;
        public DynamicCubeMapDemo(IntPtr hInstance) : base(hInstance) {
            _lightCount = 3;
            _lastMousePos = new Point();
            MainWindowCaption = "Dynamic CubeMap Demo";

            _camera = new FpsCamera { Position = new Vector3(0, 2, -15) };

            BuildCubeFaceCamera(0.0f, 2.0f, 0.0f);
            for (int i = 0; i < 6; i++) {
                _dynamicCubeMapRTV[i] = null;
            }
            _gridWorld = Matrix.Identity;

            _boxWorld = Matrix.Scaling(3.0f, 1.0f, 3.0f) * Matrix.Translation(0, 0.5f, 0);
            _centerSphereWorld = Matrix.Scaling(new Vector3(2.0f)) * Matrix.Translation(0, 2, 0);

            //_skullWorld = Matrix.Scaling(0.5f, 0.5f, 0.5f) * Matrix.Translation(0, 1.0f, 0);
            for (var i = 0; i < 5; i++) {
                _cylWorld[i * 2] = Matrix.Translation(-5.0f, 1.5f, -10.0f + i * 5.0f);
                _cylWorld[i * 2 + 1] = Matrix.Translation(5.0f, 1.5f, -10.0f + i * 5.0f);

                _sphereWorld[i * 2] = Matrix.Translation(-5.0f, 3.5f, -10.0f + i * 5.0f);
                _sphereWorld[i * 2 + 1] = Matrix.Translation(5.0f, 3.5f, -10.0f + i * 5.0f);
            }
            _dirLights = new[] {
                new DirectionalLight {
                    Ambient = new Color4( 0.2f, 0.2f, 0.2f),
                    Diffuse = new Color4(0.5f, 0.5f, 0.5f),
                    Specular = new Color4(0.5f, 0.5f, 0.5f),
                    Direction = new Vector3(0.57735f, -0.57735f, 0.57735f)
                },
                new DirectionalLight {
                    Ambient = new Color4(0,0,0),
                    Diffuse = new Color4(1.0f, 0.2f, 0.2f, 0.2f),
                    Specular = new Color4(1.0f, 0.25f, 0.25f, 0.25f),
                    Direction = new Vector3(-0.57735f, -0.57735f, 0.57735f)
                },
                new DirectionalLight {
                    Ambient = new Color4(0,0,0),
                    Diffuse = new Color4(1.0f,0.2f, 0.2f, 0.2f),
                    Specular = new Color4(0,0,0),
                    Direction = new Vector3(0, -0.707f, -0.707f)
                }
            };
            _gridMat = new Material {
                Ambient = new Color4(0.8f, 0.8f, 0.8f),
                Diffuse = new Color4(0.8f, 0.8f, 0.8f),
                Specular = new Color4(16.0f, 0.8f, 0.8f, 0.8f),
                Reflect = Color.Black
            };
            _cylinderMat = new Material {
                Ambient = Color.White,
                Diffuse = Color.White,
                Specular = new Color4(16.0f, 0.8f, 0.8f, 0.8f),
                Reflect = Color.Black
            };
            _sphereMat = new Material {
                Ambient = new Color4(0.6f, 0.8f, 0.9f),
                Diffuse = new Color4(0.6f, 0.8f, 0.9f),
                Specular = new Color4(16.0f, 0.9f, 0.9f, 0.9f),
                Reflect = Color.Black
            };
            _boxMat = new Material {
                Ambient = Color.White,
                Diffuse = Color.White,
                Specular = new Color4(16.0f, 0.8f, 0.8f, 0.8f),
                Reflect = Color.Black
            };
            _skullMat = new Material {
                Ambient = new Color4(0.4f, 0.4f, 0.4f),
                Diffuse = new Color4(0.8f, 0.8f, 0.8f),
                Specular = new Color4(16.0f, 0.8f, 0.8f, 0.8f),
                Reflect = Color.Black
            };
            _centerSphereMat = new Material {
                Ambient = new Color4(0.2f, 0.2f, 0.2f),
                Diffuse = new Color4(0.2f, 0.2f, 0.2f),
                Specular = new Color4(16.0f, 0.8f, 0.8f, 0.8f),
                Reflect = new Color4(0.8f, 0.8f, 0.8f)
            };

        }

        private void BuildCubeFaceCamera(float x, float y, float z) {
            var center = new Vector3(x, y, z);
            var worldUp = new Vector3(0, 1, 0);
            var targets = new[] {
                new Vector3(x + 1, y, z),
                new Vector3(x - 1, y, z),
                new Vector3(x, y + 1, z),
                new Vector3(x, y - 1, z),
                new Vector3(x, y, z + 1),
                new Vector3(x, y, z - 1)
            };
            var ups = new[] {
                new Vector3(0, 1, 0),
                new Vector3(0, 1, 0),
                new Vector3(0, 0, -1),
                new Vector3(0, 0, 1),
                new Vector3(0, 1, 0),
                new Vector3(0, 1, 0),
            };
            for (int i = 0; i < 6; i++) {
                _cubeMapCamera[i] = new FpsCamera();
                _cubeMapCamera[i].LookAt(center, targets[i], ups[i]);
                _cubeMapCamera[i].SetLens(MathF.PI/2, 1.0f, 0.1f, 1000.0f);
                _cubeMapCamera[i].UpdateViewMatrix();
            }
        }

        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Util.ReleaseCom(ref _sky);
                    Util.ReleaseCom(ref _shapesVB);
                    Util.ReleaseCom(ref _shapesIB);
                    Util.ReleaseCom(ref _skullVB);
                    Util.ReleaseCom(ref _skullIB);

                    Util.ReleaseCom(ref _floorTexSRV);
                    Util.ReleaseCom(ref _stoneTexSRV);
                    Util.ReleaseCom(ref _brickTexSRV);

                    Util.ReleaseCom(ref _dynamicCubeMapDSV);
                    Util.ReleaseCom(ref _dynamicCubeMapSRV);
                    for (int i = 0; i < _dynamicCubeMapRTV.Length; i++) {
                        Util.ReleaseCom(ref _dynamicCubeMapRTV[i]);
                    }

                    Effects.DestroyAll();
                    InputLayouts.DestroyAll();
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }
        public override bool Init() {
            if (!base.Init()) return false;

            Effects.InitAll(Device);
            InputLayouts.InitAll(Device);

            _sky = new Sky(Device, "Textures/sunsetcube1024.dds", 5000.0f);

            _floorTexSRV = ShaderResourceView.FromFile(Device, "Textures/floor.dds");
            _stoneTexSRV = ShaderResourceView.FromFile(Device, "Textures/stone.dds");
            _brickTexSRV = ShaderResourceView.FromFile(Device, "Textures/bricks.dds");

            BuildDynamicCubeMapViews();

            BuildShapeGeometryBuffers();
            BuildSkullGeometryBuffers();

            Window.KeyDown += SwitchLights;

            return true;
        }

        private void BuildDynamicCubeMapViews() {
            // create the render target cube map texture
            var texDesc = new Texture2DDescription() {
                Width = CubeMapSize,
                Height = CubeMapSize,
                MipLevels = 0,
                ArraySize = 6,
                SampleDescription = new SampleDescription(1, 0),
                Format = Format.R8G8B8A8_UNorm,
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.GenerateMipMaps | ResourceOptionFlags.TextureCube
            };
            var cubeTex = new Texture2D(Device, texDesc);
            cubeTex.DebugName = "dynamic cubemap texture";
            // create the render target view array
            var rtvDesc = new RenderTargetViewDescription() {
                Format = texDesc.Format,
                Dimension = RenderTargetViewDimension.Texture2DArray,
                ArraySize = 1,
                MipSlice = 0
            };
            for (int i = 0; i < 6; i++) {
                rtvDesc.FirstArraySlice = i;
                _dynamicCubeMapRTV[i] = new RenderTargetView(Device, cubeTex, rtvDesc);
            }
            // Create the shader resource view that we will bind to our effect for the cubemap
            var srvDesc = new ShaderResourceViewDescription() {
                Format = texDesc.Format,
                Dimension = ShaderResourceViewDimension.TextureCube,
                MostDetailedMip = 0,
                MipLevels = -1
            };
            _dynamicCubeMapSRV = new ShaderResourceView(Device, cubeTex, srvDesc);
            // release the texture, now that it is saved to the views
            Util.ReleaseCom(ref cubeTex);
            // create the depth/stencil texture
            var depthTexDesc = new Texture2DDescription() {
                Width = CubeMapSize,
                Height = CubeMapSize,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = new SampleDescription(1, 0),
                Format = Format.D32_Float,
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };
            var depthTex = new Texture2D(Device, depthTexDesc);
            depthTex.DebugName = "dynamic cubemap depthmap";
            var dsvDesc = new DepthStencilViewDescription() {
                Format = depthTexDesc.Format,
                Flags = DepthStencilViewFlags.None,
                Dimension = DepthStencilViewDimension.Texture2D,
                MipSlice = 0,

            };
            _dynamicCubeMapDSV = new DepthStencilView(Device, depthTex, dsvDesc);

            Util.ReleaseCom(ref depthTex);
            // create the viewport for rendering the cubemap faces
            _cubeMapViewPort = new Viewport(0, 0, CubeMapSize, CubeMapSize, 0, 1.0f);
        }

        private void SwitchLights(object sender, KeyEventArgs e) {
            switch (e.KeyCode) {
                case Keys.D0:
                    _lightCount = 0;
                    break;
                case Keys.D1:
                    _lightCount = 1;
                    break;
                case Keys.D2:
                    _lightCount = 2;
                    break;
                case Keys.D3:
                    _lightCount = 3;
                    break;
            }
        }

        private void BuildSkullGeometryBuffers() {
            try {
                var vertices = new List<Basic32>();
                var indices = new List<int>();
                var vcount = 0;
                var tcount = 0;
                using (var reader = new StreamReader("Models\\skull.txt")) {


                    var input = reader.ReadLine();
                    if (input != null)
                        // VertexCount: X
                        vcount = Convert.ToInt32(input.Split(new[] { ':' })[1].Trim());

                    input = reader.ReadLine();
                    if (input != null)
                        //TriangleCount: X
                        tcount = Convert.ToInt32(input.Split(new[] { ':' })[1].Trim());

                    // skip ahead to the vertex data
                    do {
                        input = reader.ReadLine();
                    } while (input != null && !input.StartsWith("{"));
                    // Get the vertices  
                    for (int i = 0; i < vcount; i++) {
                        input = reader.ReadLine();
                        if (input != null) {
                            var vals = input.Split(new[] { ' ' });
                            vertices.Add(
                                new Basic32(
                                    new Vector3(
                                        Convert.ToSingle(vals[0].Trim()),
                                        Convert.ToSingle(vals[1].Trim()),
                                        Convert.ToSingle(vals[2].Trim())),
                                    new Vector3(
                                        Convert.ToSingle(vals[3].Trim()),
                                        Convert.ToSingle(vals[4].Trim()),
                                        Convert.ToSingle(vals[5].Trim())),
                                        new Vector2()
                                )
                            );
                        }
                    }
                    // skip ahead to the index data
                    do {
                        input = reader.ReadLine();
                    } while (input != null && !input.StartsWith("{"));
                    // Get the indices
                    _skullIndexCount = 3 * tcount;
                    for (var i = 0; i < tcount; i++) {
                        input = reader.ReadLine();
                        if (input == null) {
                            break;
                        }
                        var m = input.Trim().Split(new[] { ' ' });
                        indices.Add(Convert.ToInt32(m[0].Trim()));
                        indices.Add(Convert.ToInt32(m[1].Trim()));
                        indices.Add(Convert.ToInt32(m[2].Trim()));
                    }
                }

                var vbd = new BufferDescription(VertexPN.Stride * vcount, ResourceUsage.Immutable,
                    BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
                _skullVB = new Buffer(Device, new DataStream(vertices.ToArray(), false, false), vbd);

                var ibd = new BufferDescription(sizeof(int) * _skullIndexCount, ResourceUsage.Immutable,
                    BindFlags.IndexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
                _skullIB = new Buffer(Device, new DataStream(indices.ToArray(), false, false), ibd);


            } catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }

        private void BuildShapeGeometryBuffers() {
            var box = GeometryGenerator.CreateBox(1, 1, 1);
            var grid = GeometryGenerator.CreateGrid(20, 30, 60, 40);
            var sphere = GeometryGenerator.CreateSphere(0.5f, 20, 20);
            var cylinder = GeometryGenerator.CreateCylinder(0.5f, 0.3f, 3.0f, 20, 20);

            _boxVertexOffset = 0;
            _gridVertexOffset = box.Vertices.Count;
            _sphereVertexOffset = _gridVertexOffset + grid.Vertices.Count;
            _cylinderVertexOffset = _sphereVertexOffset + sphere.Vertices.Count;

            _boxIndexCount = box.Indices.Count;
            _gridIndexCount = grid.Indices.Count;
            _sphereIndexCount = sphere.Indices.Count;
            _cylinderIndexCount = cylinder.Indices.Count;

            _boxIndexOffset = 0;
            _gridIndexOffset = _boxIndexCount;
            _sphereIndexOffset = _gridIndexOffset + _gridIndexCount;
            _cylinderIndexOffset = _sphereIndexOffset + _sphereIndexCount;

            var totalVertexCount = box.Vertices.Count + grid.Vertices.Count + sphere.Vertices.Count + cylinder.Vertices.Count;
            var totalIndexCount = _boxIndexCount + _gridIndexCount + _sphereIndexCount + _cylinderIndexCount;

            var vertices = box.Vertices.Select(v => new Basic32(v.Position, v.Normal, v.TexC)).ToList();
            vertices.AddRange(grid.Vertices.Select(v => new Basic32(v.Position, v.Normal, v.TexC)));
            vertices.AddRange(sphere.Vertices.Select(v => new Basic32(v.Position, v.Normal, v.TexC)));
            vertices.AddRange(cylinder.Vertices.Select(v => new Basic32(v.Position, v.Normal, v.TexC)));

            var vbd = new BufferDescription(Basic32.Stride * totalVertexCount, ResourceUsage.Immutable, BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _shapesVB = new Buffer(Device, new DataStream(vertices.ToArray(), false, false), vbd);

            var indices = new List<int>();
            indices.AddRange(box.Indices);
            indices.AddRange(grid.Indices);
            indices.AddRange(sphere.Indices);
            indices.AddRange(cylinder.Indices);

            var ibd = new BufferDescription(sizeof(int) * totalIndexCount, ResourceUsage.Immutable, BindFlags.IndexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _shapesIB = new Buffer(Device, new DataStream(indices.ToArray(), false, false), ibd);
        }

        public override void OnResize() {
            base.OnResize();
            _camera.SetLens(0.25f * MathF.PI, AspectRatio, 1.0f, 1000.0f);
        }
        public override void UpdateScene(float dt) {
            base.UpdateScene(dt);
            if (Util.IsKeyDown(Keys.Up)) {
                _camera.Walk(10.0f * dt);
            }
            if (Util.IsKeyDown(Keys.Down)) {
                _camera.Walk(-10.0f * dt);
            }
            if (Util.IsKeyDown(Keys.Left)) {
                _camera.Strafe(-10.0f * dt);
            }
            if (Util.IsKeyDown(Keys.Right)) {
                _camera.Strafe(10.0f * dt);
            }
            if (Util.IsKeyDown(Keys.PageUp)) {
                _camera.Zoom(-dt);
            }
            if (Util.IsKeyDown(Keys.PageDown)) {
                _camera.Zoom(+dt);
            }
            var skullScale = Matrix.Scaling(0.2f, 0.2f, 0.2f);
            var skullOffset = Matrix.Translation(3, 2, 0);
            var skullLocalRotate = Matrix.RotationY(2.0f * Timer.TotalTime);
            var skullGlobalRotate = Matrix.RotationY(0.5f * Timer.TotalTime);
            _skullWorld = skullScale * skullLocalRotate * skullOffset * skullGlobalRotate;

            _camera.UpdateViewMatrix();
        }
        public override void DrawScene() {
            base.DrawScene();
            ImmediateContext.Rasterizer.SetViewports(_cubeMapViewPort);
            for (int i = 0; i < 6; i++) {
                ImmediateContext.ClearRenderTargetView(_dynamicCubeMapRTV[i], Color.Silver);
                ImmediateContext.ClearDepthStencilView(
                    _dynamicCubeMapDSV, 
                    DepthStencilClearFlags.Depth| 
                    DepthStencilClearFlags.Stencil, 
                    1.0f, 0 );
                ImmediateContext.OutputMerger.SetTargets(
                    _dynamicCubeMapDSV,
                    _dynamicCubeMapRTV[i]
                );
                DrawScene(_cubeMapCamera[i], false);
            }
            ImmediateContext.Rasterizer.SetViewports(Viewport);
            ImmediateContext.OutputMerger.SetTargets(DepthStencilView, RenderTargetView);

            ImmediateContext.GenerateMips(_dynamicCubeMapSRV);

            ImmediateContext.ClearRenderTargetView(RenderTargetView, Color.Silver);
            ImmediateContext.ClearDepthStencilView(DepthStencilView, 
                DepthStencilClearFlags.Depth|DepthStencilClearFlags.Stencil, 
                1.0f, 0 );

            DrawScene(_camera, true);
            SwapChain.Present(0, PresentFlags.None);
        }
        protected override void OnMouseDown(object sender, MouseEventArgs mouseEventArgs) {
            _lastMousePos = mouseEventArgs.Location;
            Window.Capture = true;
        }
        protected override void OnMouseUp(object sender, MouseEventArgs e) {
            Window.Capture = false;
        }
        protected override void OnMouseMove(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                var dx = MathF.ToRadians(0.25f * (e.X - _lastMousePos.X));
                var dy = MathF.ToRadians(0.25f * (e.Y - _lastMousePos.Y));

                _camera.Pitch(dy);
                _camera.Yaw(dx);

            }
            _lastMousePos = e.Location;
        }

        private void DrawScene(CameraBase camera, bool drawCenterSphere) {
            ImmediateContext.InputAssembler.InputLayout = InputLayouts.Basic32;
            ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            

            Matrix view = camera.View;
            Matrix proj = camera.Proj;
            Matrix viewProj = camera.ViewProj;
            Effects.BasicFX.SetEyePosW(camera.Position);
            Effects.BasicFX.SetDirLights(_dirLights);

            var activeTexTech = Effects.BasicFX.Light1TexTech;
            var activeSkullTech = Effects.BasicFX.Light1ReflectTech;
            var activeReflectTech = Effects.BasicFX.Light1TexReflectTech;
            switch (_lightCount) {
                case 1:
                    activeTexTech = Effects.BasicFX.Light1TexTech;
                    activeSkullTech = Effects.BasicFX.Light1ReflectTech;
                    activeReflectTech = Effects.BasicFX.Light1TexReflectTech;
                    break;
                case 2:
                    activeTexTech = Effects.BasicFX.Light2TexTech;
                    activeSkullTech = Effects.BasicFX.Light2ReflectTech;
                    activeReflectTech = Effects.BasicFX.Light2TexReflectTech;
                    break;
                case 3:
                    activeTexTech = Effects.BasicFX.Light3TexTech;
                    activeSkullTech = Effects.BasicFX.Light3ReflectTech;
                    activeReflectTech = Effects.BasicFX.Light3TexReflectTech;
                    break;
            }
            for (int p = 0; p < activeSkullTech.Description.PassCount; p++) {
                var pass = activeSkullTech.GetPassByIndex(p);
                ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_skullVB, Basic32.Stride, 0));
                ImmediateContext.InputAssembler.SetIndexBuffer(_skullIB, Format.R32_UInt, 0);

                var world = _skullWorld;
                var worldInvTranspose = MathF.InverseTranspose(world);
                var wvp = world * viewProj;
                Effects.BasicFX.SetWorld(world);
                Effects.BasicFX.SetWorldInvTranspose(worldInvTranspose);
                Effects.BasicFX.SetTexTransform(Matrix.Identity);
                Effects.BasicFX.SetWorldViewProj(wvp);
                Effects.BasicFX.SetMaterial(_skullMat);
                pass.Apply(ImmediateContext);
                ImmediateContext.DrawIndexed(_skullIndexCount, 0, 0);
            }
            // draw grid, box, cylinders without reflection
            for (var p = 0; p < activeTexTech.Description.PassCount; p++) {
                var pass = activeTexTech.GetPassByIndex(p);
                ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_shapesVB, Basic32.Stride, 0));
                ImmediateContext.InputAssembler.SetIndexBuffer(_shapesIB, Format.R32_UInt, 0);

                var world = _gridWorld;
                var worldInvTranspose = MathF.InverseTranspose(world);
                var wvp = world * view * proj;
                Effects.BasicFX.SetWorld(world);
                Effects.BasicFX.SetWorldInvTranspose(worldInvTranspose);
                Effects.BasicFX.SetWorldViewProj(wvp);
                Effects.BasicFX.SetTexTransform(Matrix.Scaling(6, 8, 1));
                Effects.BasicFX.SetMaterial(_gridMat);
                Effects.BasicFX.SetDiffuseMap(_floorTexSRV);
                pass.Apply(ImmediateContext);
                ImmediateContext.DrawIndexed(_gridIndexCount, _gridIndexOffset, _gridVertexOffset);

                world = _boxWorld;
                worldInvTranspose = MathF.InverseTranspose(world);
                wvp = world * viewProj;
                Effects.BasicFX.SetWorld(world);
                Effects.BasicFX.SetWorldInvTranspose(worldInvTranspose);
                Effects.BasicFX.SetWorldViewProj(wvp);
                Effects.BasicFX.SetTexTransform(Matrix.Identity);
                Effects.BasicFX.SetMaterial(_boxMat);
                Effects.BasicFX.SetDiffuseMap(_stoneTexSRV);
                pass.Apply(ImmediateContext);
                ImmediateContext.DrawIndexed(_boxIndexCount, _boxIndexOffset, _boxVertexOffset);

                foreach (var matrix in _cylWorld) {
                    world = matrix;
                    worldInvTranspose = MathF.InverseTranspose(world);
                    wvp = world * viewProj;
                    Effects.BasicFX.SetWorld(world);
                    Effects.BasicFX.SetWorldInvTranspose(worldInvTranspose);
                    Effects.BasicFX.SetWorldViewProj(wvp);
                    Effects.BasicFX.SetTexTransform(Matrix.Identity);
                    Effects.BasicFX.SetMaterial(_cylinderMat);
                    Effects.BasicFX.SetDiffuseMap(_brickTexSRV);
                    pass.Apply(ImmediateContext);
                    ImmediateContext.DrawIndexed(_cylinderIndexCount, _cylinderIndexOffset, _cylinderVertexOffset);
                }
                foreach (var matrix in _sphereWorld) {
                    world = matrix;
                    worldInvTranspose = MathF.InverseTranspose(world);
                    wvp = world * viewProj;
                    Effects.BasicFX.SetWorld(world);
                    Effects.BasicFX.SetWorldInvTranspose(worldInvTranspose);
                    Effects.BasicFX.SetWorldViewProj(wvp);
                    Effects.BasicFX.SetTexTransform(Matrix.Identity);
                    Effects.BasicFX.SetMaterial(_sphereMat);
                    Effects.BasicFX.SetDiffuseMap(_stoneTexSRV);
                    pass.Apply(ImmediateContext);
                    ImmediateContext.DrawIndexed(_sphereIndexCount, _sphereIndexOffset, _sphereVertexOffset);
                }
            }
            if (drawCenterSphere) {
                for (int p = 0; p < activeReflectTech.Description.PassCount; p++) {
                    var pass = activeReflectTech.GetPassByIndex(p);
                    var world = _centerSphereWorld;
                    var wit = MathF.InverseTranspose(world);
                    var wvp = world * viewProj;
                    Effects.BasicFX.SetWorld(world);
                    Effects.BasicFX.SetWorldInvTranspose(wit);
                    Effects.BasicFX.SetWorldViewProj(wvp);
                    Effects.BasicFX.SetTexTransform(Matrix.Identity);
                    Effects.BasicFX.SetMaterial(_centerSphereMat);
                    Effects.BasicFX.SetDiffuseMap(_stoneTexSRV);
                    Effects.BasicFX.SetCubeMap(_dynamicCubeMapSRV);

                    pass.Apply(ImmediateContext);
                    ImmediateContext.DrawIndexed(_sphereIndexCount, _sphereIndexOffset, _sphereVertexOffset);

                }
            }
            _sky.Draw(ImmediateContext, camera);

            ImmediateContext.Rasterizer.State = null;
            ImmediateContext.OutputMerger.DepthStencilState = null;
            ImmediateContext.OutputMerger.DepthStencilReference = 0;
        }
    }

    class Program {
        static void Main(string[] args) {
            Configuration.EnableObjectTracking = true;
            var app = new DynamicCubeMapDemo(Process.GetCurrentProcess().Handle);
            if (!app.Init()) {
                return;
            }
            app.Run();
        }
    }
}
