using System;
using System.Collections.Generic;
using System.Linq;

namespace NormalDisplacementMaps {
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

    enum RenderOptions {
        Basic,
        NormalMap,
        DisplacementMap
    }

    class NormalDisplacementMapsDemo : D3DApp {
        private Sky _sky;

        private Buffer _shapesVB;
        private Buffer _shapesIB;

        private Buffer _skullVB;
        private Buffer _skullIB;

        private ShaderResourceView _stoneTexSRV;
        private ShaderResourceView _brickTexSRV;

        private ShaderResourceView _stoneNormalTexSRV;
        private ShaderResourceView _brickNormalTexSRV;

        private readonly DirectionalLight[] _dirLights;
        private readonly Material _gridMat;
        private readonly Material _boxMat;
        private readonly Material _cylinderMat;
        private readonly Material _sphereMat;
        private readonly Material _skullMat;

        private readonly Matrix[] _sphereWorld = new Matrix[10];
        private readonly Matrix[] _cylWorld = new Matrix[10];
        private readonly Matrix _boxWorld;
        private readonly Matrix _gridWorld;
        private readonly Matrix _skullWorld;

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

        private RenderOptions _renderOptions;
        private readonly FpsCamera _camera;
        private Point _lastMousePos;
        private bool _disposed;

        protected NormalDisplacementMapsDemo(IntPtr hInstance) : base(hInstance) {
            _renderOptions = RenderOptions.NormalMap;
            MainWindowCaption = "Normal-Displacement Map Demo";

            Enable4xMsaa = true;
            _lastMousePos = new Point();

            _camera = new FpsCamera { Position = new Vector3(0, 2, -15) };
            _gridWorld = Matrix.Identity;

            _boxWorld = Matrix.Scaling(3.0f, 1.0f, 3.0f) * Matrix.Translation(0, 0.5f, 0);
            _skullWorld = Matrix.Scaling(0.5f, 0.5f, 0.5f) * Matrix.Translation(0, 1.0f, 0);

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
                Reflect = new Color4(0.4f, 0.4f, 0.4f)
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
                Reflect = new Color4(0.5f, 0.5f, 0.5f)
            };
        }
        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Util.ReleaseCom(ref _sky);
                    Util.ReleaseCom(ref _shapesVB);
                    Util.ReleaseCom(ref _shapesIB);
                    Util.ReleaseCom(ref _skullVB);
                    Util.ReleaseCom(ref _skullIB);

                    
                    Util.ReleaseCom(ref _stoneTexSRV);
                    Util.ReleaseCom(ref _brickTexSRV);

                    Util.ReleaseCom(ref _stoneNormalTexSRV);
                    Util.ReleaseCom(ref _brickNormalTexSRV);

                    

                    Effects.DestroyAll();
                    InputLayouts.DestroyAll();
                    RenderStates.DestroyAll();
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }
        public override bool Init() {
            if (!base.Init()) return false;
            Effects.InitAll(Device);
            InputLayouts.InitAll(Device);
            RenderStates.InitAll(Device);

            _sky = new Sky(Device, "Textures/snowcube1024.dds", 5000.0f);

            _stoneTexSRV = ShaderResourceView.FromFile(Device, "Textures/floor.dds");
            _brickTexSRV = ShaderResourceView.FromFile(Device, "Textures/bricks.dds");
            _stoneNormalTexSRV = ShaderResourceView.FromFile(Device, "Textures/floor_nmap.dds");
            _brickNormalTexSRV = ShaderResourceView.FromFile(Device, "Textures/bricks_nmap.dds");

            BuildShapeGeometryBuffers();
            BuildSkullGeometryBuffers();

            Window.KeyDown += SwitchMode;

            return true;
        }
        private void SwitchMode(object sender, KeyEventArgs e) {
            switch (e.KeyCode) {
                case Keys.D0:
                    _renderOptions = RenderOptions.Basic;
                    break;
                case Keys.D1:
                    _renderOptions = RenderOptions.NormalMap;
                    break;
                case Keys.D2:
                    _renderOptions = RenderOptions.DisplacementMap;
                    break;
            }
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
            
        }

        public override void DrawScene() {
            base.DrawScene();

            ImmediateContext.ClearRenderTargetView(RenderTargetView, Color.Silver);
            ImmediateContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth|DepthStencilClearFlags.Stencil, 1.0f, 0 );

            _camera.UpdateViewMatrix();
            
            var view = _camera.View;
            var proj = _camera.Proj;
            var viewProj = _camera.ViewProj;

            var blendFactor = new Color4(0, 0, 0, 0);
            Effects.BasicFX.SetDirLights(_dirLights);
            Effects.BasicFX.SetEyePosW(_camera.Position);
            Effects.BasicFX.SetCubeMap(_sky.CubeMapSRV);

            Effects.NormalMapFX.SetDirLights(_dirLights);
            Effects.NormalMapFX.SetEyePosW(_camera.Position);
            Effects.NormalMapFX.SetCubeMap(_sky.CubeMapSRV);

            Effects.DisplacementMapFX.SetDirLights(_dirLights);
            Effects.DisplacementMapFX.SetEyePosW(_camera.Position);
            Effects.DisplacementMapFX.SetCubeMap(_sky.CubeMapSRV);

            Effects.DisplacementMapFX.SetHeightScale(0.05f);
            Effects.DisplacementMapFX.SetMaxTessDistance(1.0f);
            Effects.DisplacementMapFX.SetMinTessDistance(25.0f);
            Effects.DisplacementMapFX.SetMinTessFactor(1.0f);
            Effects.DisplacementMapFX.SetMaxTessFactor(5.0f);

            var activeTech = Effects.DisplacementMapFX.Light3Tech;
            var activeSphereTech = Effects.BasicFX.Light3ReflectTech;
            var activeSkullTech = Effects.BasicFX.Light3ReflectTech;

            switch (_renderOptions) {
                case RenderOptions.Basic:
                    activeTech = Effects.BasicFX.Light3TexTech;
                    ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
                    break;
                case RenderOptions.NormalMap:
                    activeTech = Effects.NormalMapFX.Light3TexTech;
                    ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
                    break;
                case RenderOptions.DisplacementMap:
                    activeTech = Effects.DisplacementMapFX.Light3TexTech;
                    ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.PatchListWith3ControlPoints;
                    break;
            }
            var stride = PosNormalTexTan.Stride;
            var offset = 0;

            ImmediateContext.InputAssembler.InputLayout = InputLayouts.PosNormalTexTan;
            ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_shapesVB, stride, offset));
            ImmediateContext.InputAssembler.SetIndexBuffer(_shapesIB, Format.R32_UInt, 0);

            if (Util.IsKeyDown(Keys.W)) {
                ImmediateContext.Rasterizer.State = RenderStates.WireframeRS;
            }
            for (int p = 0; p < activeTech.Description.PassCount; p++) {
                var world = _gridWorld;
                var wit = MathF.InverseTranspose(world);
                var wvp = world * viewProj;

                switch (_renderOptions) {
                    case RenderOptions.Basic:
                        Effects.BasicFX.SetWorld(world);
                        Effects.BasicFX.SetWorldInvTranspose(wit);
                        Effects.BasicFX.SetWorldViewProj(wvp);
                        Effects.BasicFX.SetTexTransform(Matrix.Scaling(8, 10, 1));
                        Effects.BasicFX.SetMaterial(_gridMat);
                        Effects.BasicFX.SetDiffuseMap(_stoneTexSRV);
                        break;
                    case RenderOptions.NormalMap:
                        Effects.NormalMapFX.SetWorld(world);
                        Effects.NormalMapFX.SetWorldInvTranspose(wit);
                        Effects.NormalMapFX.SetWorldViewProj(wvp);
                        Effects.NormalMapFX.SetTexTransform(Matrix.Scaling(8, 10, 1));
                        Effects.NormalMapFX.SetMaterial(_gridMat);
                        Effects.NormalMapFX.SetDiffuseMap(_stoneTexSRV);
                        Effects.NormalMapFX.SetNormalMap(_stoneNormalTexSRV);
                        break;
                    case RenderOptions.DisplacementMap:
                        Effects.DisplacementMapFX.SetWorld(world);
                        Effects.DisplacementMapFX.SetWorldInvTranspose(wit);
                        Effects.DisplacementMapFX.SetViewProj(viewProj);
                        Effects.DisplacementMapFX.SetWorldViewProj(wvp);
                        Effects.DisplacementMapFX.SetTexTransform(Matrix.Scaling(8, 10, 1));
                        Effects.DisplacementMapFX.SetMaterial(_gridMat);
                        Effects.DisplacementMapFX.SetDiffuseMap(_stoneTexSRV);
                        Effects.DisplacementMapFX.SetNormalMap(_stoneNormalTexSRV);
                        break;
                }
                var pass = activeTech.GetPassByIndex(p);
                pass.Apply(ImmediateContext);
                ImmediateContext.DrawIndexed(_gridIndexCount, _gridIndexOffset, _gridVertexOffset);

                world = _boxWorld;
                wit = MathF.InverseTranspose(world);
                wvp = world * viewProj;

                switch (_renderOptions) {
                    case RenderOptions.Basic:
                        Effects.BasicFX.SetWorld(world);
                        Effects.BasicFX.SetWorldInvTranspose(wit);
                        Effects.BasicFX.SetWorldViewProj(wvp);
                        Effects.BasicFX.SetTexTransform(Matrix.Scaling(2, 1, 1));
                        Effects.BasicFX.SetMaterial(_boxMat);
                        Effects.BasicFX.SetDiffuseMap(_brickTexSRV);
                        break;
                    case RenderOptions.NormalMap:
                        Effects.NormalMapFX.SetWorld(world);
                        Effects.NormalMapFX.SetWorldInvTranspose(wit);
                        Effects.NormalMapFX.SetWorldViewProj(wvp);
                        Effects.NormalMapFX.SetTexTransform(Matrix.Scaling(2, 1, 1));
                        Effects.NormalMapFX.SetMaterial(_boxMat);
                        Effects.NormalMapFX.SetDiffuseMap(_brickTexSRV);
                        Effects.NormalMapFX.SetNormalMap(_brickNormalTexSRV);
                        break;
                    case RenderOptions.DisplacementMap:
                        Effects.DisplacementMapFX.SetWorld(world);
                        Effects.DisplacementMapFX.SetWorldInvTranspose(wit);
                        Effects.DisplacementMapFX.SetViewProj(viewProj);
                        Effects.DisplacementMapFX.SetWorldViewProj(wvp);
                        Effects.DisplacementMapFX.SetTexTransform(Matrix.Scaling(2, 1, 1));
                        Effects.DisplacementMapFX.SetMaterial(_boxMat);
                        Effects.DisplacementMapFX.SetDiffuseMap(_brickTexSRV);
                        Effects.DisplacementMapFX.SetNormalMap(_brickNormalTexSRV);
                        break;
                }
                pass.Apply(ImmediateContext);
                ImmediateContext.DrawIndexed(_boxIndexCount, _boxIndexOffset, _boxVertexOffset);

                foreach (var matrix in _cylWorld) {
                    world = matrix;
                    wit = MathF.InverseTranspose(world);
                    wvp = world * viewProj;

                    switch (_renderOptions) {
                        case RenderOptions.Basic:
                            Effects.BasicFX.SetWorld(world);
                            Effects.BasicFX.SetWorldInvTranspose(wit);
                            Effects.BasicFX.SetWorldViewProj(wvp);
                            Effects.BasicFX.SetTexTransform(Matrix.Scaling(1,2, 1));
                            Effects.BasicFX.SetMaterial(_cylinderMat);
                            Effects.BasicFX.SetDiffuseMap(_brickTexSRV);
                            break;
                        case RenderOptions.NormalMap:
                            Effects.NormalMapFX.SetWorld(world);
                            Effects.NormalMapFX.SetWorldInvTranspose(wit);
                            Effects.NormalMapFX.SetWorldViewProj(wvp);
                            Effects.NormalMapFX.SetTexTransform(Matrix.Scaling(1, 2, 1));
                            Effects.NormalMapFX.SetMaterial(_cylinderMat);
                            Effects.NormalMapFX.SetDiffuseMap(_brickTexSRV);
                            Effects.NormalMapFX.SetNormalMap(_brickNormalTexSRV);
                            break;
                        case RenderOptions.DisplacementMap:
                            Effects.DisplacementMapFX.SetWorld(world);
                            Effects.DisplacementMapFX.SetWorldInvTranspose(wit);
                            Effects.DisplacementMapFX.SetViewProj(viewProj);
                            Effects.DisplacementMapFX.SetWorldViewProj(wvp);
                            Effects.DisplacementMapFX.SetTexTransform(Matrix.Scaling(1, 2, 1));
                            Effects.DisplacementMapFX.SetMaterial(_cylinderMat);
                            Effects.DisplacementMapFX.SetDiffuseMap(_brickTexSRV);
                            Effects.DisplacementMapFX.SetNormalMap(_brickNormalTexSRV);
                            break;
                    }
                    pass.Apply(ImmediateContext);
                    ImmediateContext.DrawIndexed(_cylinderIndexCount, _cylinderIndexOffset, _cylinderVertexOffset);
                }
                
            }
            ImmediateContext.HullShader.Set(null);
            ImmediateContext.DomainShader.Set(null);
            ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            for (int p = 0; p < activeSphereTech.Description.PassCount; p++) {
                foreach (var matrix in _sphereWorld) {
                    var world = matrix;
                    var wit = MathF.InverseTranspose(world);
                    var wvp = world * viewProj;

                    Effects.BasicFX.SetWorld(world);
                    Effects.BasicFX.SetWorldInvTranspose(wit);
                    Effects.BasicFX.SetWorldViewProj(wvp);
                    Effects.BasicFX.SetTexTransform(Matrix.Identity);
                    Effects.BasicFX.SetMaterial(_sphereMat);

                    activeSphereTech.GetPassByIndex(p).Apply(ImmediateContext);
                    ImmediateContext.DrawIndexed(_sphereIndexCount, _sphereIndexOffset, _sphereVertexOffset);
                }
            }
            stride = Basic32.Stride;
            offset = 0;

            ImmediateContext.Rasterizer.State = null;

            ImmediateContext.InputAssembler.InputLayout = InputLayouts.Basic32;
            ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_skullVB, stride, offset));
            ImmediateContext.InputAssembler.SetIndexBuffer(_skullIB, Format.R32_UInt, 0);

            for (int p = 0; p < activeSkullTech.Description.PassCount; p++) {
                var world = _skullWorld;
                var wit = MathF.InverseTranspose(world);
                var wvp = world * viewProj;

                Effects.BasicFX.SetWorld(world);
                Effects.BasicFX.SetWorldInvTranspose(wit);
                Effects.BasicFX.SetWorldViewProj(wvp);
                Effects.BasicFX.SetMaterial(_skullMat);

                activeSkullTech.GetPassByIndex(p).Apply(ImmediateContext);
                ImmediateContext.DrawIndexed(_skullIndexCount, 0, 0);

            }
            _sky.Draw(ImmediateContext, _camera);

            ImmediateContext.Rasterizer.State = null;
            ImmediateContext.OutputMerger.DepthStencilState = null;
            ImmediateContext.OutputMerger.DepthStencilReference = 0;

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

            var vertices = box.Vertices.Select(v => new PosNormalTexTan(v.Position, v.Normal, v.TexC, v.TangentU)).ToList();
            vertices.AddRange(grid.Vertices.Select(v => new PosNormalTexTan(v.Position, v.Normal, v.TexC, v.TangentU)));
            vertices.AddRange(sphere.Vertices.Select(v => new PosNormalTexTan(v.Position, v.Normal, v.TexC, v.TangentU)));
            vertices.AddRange(cylinder.Vertices.Select(v => new PosNormalTexTan(v.Position, v.Normal, v.TexC, v.TangentU)));

            var vbd = new BufferDescription(PosNormalTexTan.Stride * totalVertexCount, ResourceUsage.Immutable, BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _shapesVB = new Buffer(Device, new DataStream(vertices.ToArray(), false, false), vbd);

            var indices = new List<int>();
            indices.AddRange(box.Indices);
            indices.AddRange(grid.Indices);
            indices.AddRange(sphere.Indices);
            indices.AddRange(cylinder.Indices);

            var ibd = new BufferDescription(sizeof(int) * totalIndexCount, ResourceUsage.Immutable, BindFlags.IndexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _shapesIB = new Buffer(Device, new DataStream(indices.ToArray(), false, false), ibd);
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

        public static void Main(string[] args) {
            Configuration.EnableObjectTracking = true;
            var app = new NormalDisplacementMapsDemo(Process.GetCurrentProcess().Handle);
            if (!app.Init()) {
                return;
            }
            app.Run();
        }
    }
}
