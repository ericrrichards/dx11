using System;
using System.Collections.Generic;
using System.Linq;

using SlimDX;
using SlimDX.Direct3D11;
using SlimDX.DXGI;

namespace SSAODemo2 {
    using System.Diagnostics;
    using System.Drawing;
    using System.Windows.Forms;

    using Core;
    using Core.Camera;
    using Core.FX;
    using Core.Model;
    using Core.Vertex;

    using Buffer = SlimDX.Direct3D11.Buffer;

    class SsaoDemo : D3DApp {

        private Sky _sky;

        private Ssao _ssao;

        private Buffer _screenQuadVB;
        private Buffer _screenQuadIB;

        private TextureManager _texMgr;

        private readonly LookAtCamera _camera;
        private Point _lastMousePos;

        private BasicModel _boxModel;
        private BasicModel _gridModel;
        private BasicModel _sphereModel;
        private BasicModel _cylinderModel;
        private BasicModel _skullModel;

        private BasicModelInstance _grid;
        private BasicModelInstance _box;
        private readonly BasicModelInstance[] _spheres = new BasicModelInstance[10];
        private readonly BasicModelInstance[] _cylinders = new BasicModelInstance[10];
        private BasicModelInstance _skull;

        private bool _disposed;

        private readonly DirectionalLight[] _dirLights;

        private SsaoDemo(IntPtr hInstance)
            : base(hInstance) {

            MainWindowCaption = "SSAO Demo";

            _lastMousePos = new Point();

            _camera = new LookAtCamera { Position = new Vector3(0, 2, -15) };


            _dirLights = new[] {
                new DirectionalLight {
                    Ambient = new Color4(1f, 1f, 1f),
                    Diffuse = new Color4(0.0f, 0.0f, 0.0f),
                    Specular = new Color4(0.0f, 0.0f, 0.0f),
                    Direction = new Vector3(-0.57735f, -0.57735f, 0.57735f)
                },
                new DirectionalLight {
                    Ambient = new Color4(0,0,0),
                    Diffuse = new Color4( 0.0f, 0.0f, 0.0f),
                    Specular = new Color4( 0.0f, 0.0f, 0.0f),
                    Direction = new Vector3(0.707f, -0.707f, 0)
                },
                new DirectionalLight {
                    Ambient = new Color4(0,0,0),
                    Diffuse = new Color4(0.0f, 0.0f, 0.0f),
                    Specular = new Color4(0.0f,0.0f,0.0f),
                    Direction = new Vector3(0, 0, -1)
                }
            };

        }
        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Util.ReleaseCom(ref _sky);
                    Util.ReleaseCom(ref _ssao);

                    Util.ReleaseCom(ref _screenQuadVB);
                    Util.ReleaseCom(ref _screenQuadIB);


                    Util.ReleaseCom(ref _texMgr);

                    Util.ReleaseCom(ref _gridModel);
                    Util.ReleaseCom(ref _boxModel);
                    Util.ReleaseCom(ref _sphereModel);
                    Util.ReleaseCom(ref _cylinderModel);
                    Util.ReleaseCom(ref _skullModel);



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

            _sky = new Sky(Device, "Textures/desertcube1024.dds", 5000.0f);

            _ssao = new Ssao(Device, ImmediateContext, ClientWidth, ClientHeight, _camera.FovY, _camera.FarZ);

            BuildShapeGeometryBuffers();
            BuildSkullGeometryBuffers();
            BuildScreenQuadGeometryBuffers();

            return true;
        }
        public override void OnResize() {
            base.OnResize();
            _camera.SetLens(0.25f * MathF.PI, AspectRatio, 1.0f, 1000.0f);

            if (_ssao != null) {
                _ssao.OnSize(ClientWidth, ClientHeight, _camera.FovY, _camera.FarZ);
            }
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
                _camera.Zoom(10*-dt);
            }
            if (Util.IsKeyDown(Keys.PageDown)) {
                _camera.Zoom(10*+dt);
            }

            _camera.UpdateViewMatrix();

        }
        public override void DrawScene() {

            ImmediateContext.Rasterizer.State = null;

            ImmediateContext.ClearDepthStencilView(
                DepthStencilView,
                DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil,
                1.0f, 0
            );
            ImmediateContext.Rasterizer.SetViewports(Viewport);

            _ssao.SetNormalDepthRenderTarget(DepthStencilView);

            DrawSceneToSsaoNormalDepthMap();

            _ssao.ComputeSsao(_camera);
            _ssao.BlurAmbientMap(4);


            ImmediateContext.OutputMerger.SetTargets(DepthStencilView, RenderTargetView);
            ImmediateContext.Rasterizer.SetViewports(Viewport);

            ImmediateContext.ClearRenderTargetView(RenderTargetView, Color.Silver);
            ImmediateContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);
            
            var viewProj = _camera.ViewProj;

            Effects.BasicFX.SetDirLights(_dirLights);
            Effects.BasicFX.SetEyePosW(_camera.Position);
            //Effects.BasicFX.SetCubeMap(_sky.CubeMapSRV);
            
            Effects.NormalMapFX.SetDirLights(_dirLights);
            Effects.NormalMapFX.SetEyePosW(_camera.Position);
            //Effects.NormalMapFX.SetCubeMap(_sky.CubeMapSRV);

            if (!Util.IsKeyDown(Keys.S)) {
                Effects.BasicFX.SetSsaoMap(_ssao.AmbientSRV);
                Effects.NormalMapFX.SetSsaoMap(_ssao.AmbientSRV);
            } else {
                Effects.BasicFX.SetSsaoMap(_texMgr.CreateTexture("Textures/white.dds"));
                Effects.NormalMapFX.SetSsaoMap(_texMgr.CreateTexture("Textures/white.dds"));
            }
            var toTexSpace = Matrix.Scaling(0.5f, -0.5f, 1.0f) * Matrix.Translation(0.5f, 0.5f, 0);

            var activeTech = Effects.NormalMapFX.Light3Tech;
            var activeSphereTech = Effects.BasicFX.Light3ReflectTech;
            var activeSkullTech = Effects.BasicFX.Light3ReflectTech;

            ImmediateContext.InputAssembler.InputLayout = InputLayouts.PosNormalTexTan;
            ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            
            if (Util.IsKeyDown(Keys.W)) {
                ImmediateContext.Rasterizer.State = RenderStates.WireframeRS;
            }
            for (var p = 0; p < activeTech.Description.PassCount; p++) {
                // draw grid
                var pass = activeTech.GetPassByIndex(p);
                _grid.ToTexSpace = toTexSpace;
                _grid.Draw(ImmediateContext, pass, viewProj);
                // draw box
                _box.ToTexSpace = toTexSpace;
                _box.Draw(ImmediateContext, pass, viewProj);

                // draw columns
                foreach (var cylinder in _cylinders) {
                    cylinder.ToTexSpace = toTexSpace;
                    cylinder.Draw(ImmediateContext, pass, viewProj);
                }

            }
            ImmediateContext.HullShader.Set(null);
            ImmediateContext.DomainShader.Set(null);
            ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            for (var p = 0; p < activeSphereTech.Description.PassCount; p++) {
                var pass = activeSphereTech.GetPassByIndex(p);

                foreach (var sphere in _spheres) {
                    sphere.ToTexSpace = toTexSpace;
                    sphere.DrawBasic(ImmediateContext, pass, viewProj);
                }

            }

            ImmediateContext.Rasterizer.State = null;

            ImmediateContext.InputAssembler.InputLayout = InputLayouts.Basic32;

            for (int p = 0; p < activeSkullTech.Description.PassCount; p++) {
                _skull.ToTexSpace = toTexSpace;
                _skull.DrawBasic(ImmediateContext, activeSkullTech.GetPassByIndex(p), viewProj);

            }
            ImmediateContext.OutputMerger.DepthStencilState = null;
            ImmediateContext.OutputMerger.DepthStencilReference = 0;
            
            DrawScreenQuad(_ssao.AmbientSRV);
            DrawScreenQuad2(_ssao.NormalDepthSRV);

            _sky.Draw(ImmediateContext, _camera);

            ImmediateContext.Rasterizer.State = null;
            ImmediateContext.OutputMerger.DepthStencilState = null;
            ImmediateContext.OutputMerger.DepthStencilReference = 0;

            var srvs = new List<ShaderResourceView>();
            for (int i = 0; i < 16; i++) {
                srvs.Add(null);
            }

            ImmediateContext.PixelShader.SetShaderResources(srvs.ToArray(), 0, 16);

            SwapChain.Present(0, PresentFlags.None);
        }
        private void DrawSceneToSsaoNormalDepthMap() {
            var view = _camera.View;
            var proj = _camera.Proj;

            var tech = Effects.SsaoNormalDepthFX.NormalDepthTech;

            ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            ImmediateContext.InputAssembler.InputLayout = InputLayouts.PosNormalTexTan;

            for (int p = 0; p < tech.Description.PassCount; p++) {
                var pass = tech.GetPassByIndex(p);
                _grid.DrawSsaoDepth(ImmediateContext, pass, view, proj);
                _box.DrawSsaoDepth(ImmediateContext, pass, view, proj);
                foreach (var cylinder in _cylinders) {
                    cylinder.DrawSsaoDepth(ImmediateContext, pass, view, proj);
                }
                foreach (var sphere in _spheres) {
                    sphere.DrawSsaoDepth(ImmediateContext, pass, view, proj);
                }

            }
            ImmediateContext.Rasterizer.State = null;
            ImmediateContext.InputAssembler.InputLayout = InputLayouts.Basic32;

            for (int p = 0; p < tech.Description.PassCount; p++) {
                _skull.DrawSsaoDepth(ImmediateContext, tech.GetPassByIndex(p), view, proj);
            }

        }
        private void DrawScreenQuad(ShaderResourceView srv) {
            var stride = Basic32.Stride;
            const int Offset = 0;

            ImmediateContext.InputAssembler.InputLayout = InputLayouts.Basic32;
            ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_screenQuadVB, stride, Offset));
            ImmediateContext.InputAssembler.SetIndexBuffer(_screenQuadIB, Format.R32_UInt, 0);


            var world = new Matrix {
                M11 = 0.25f,
                M22 = 0.25f,
                M33 = 1.0f,
                M41 = 0.75f,
                M42 = -0.75f,
                M44 = 1.0f
            };

            //var world = Matrix.Identity;
            var tech = Effects.DebugTexFX.ViewRedTech;
            for (int p = 0; p < tech.Description.PassCount; p++) {
                Effects.DebugTexFX.SetWorldViewProj(world);
                Effects.DebugTexFX.SetTexture(srv);
                tech.GetPassByIndex(p).Apply(ImmediateContext);
                ImmediateContext.DrawIndexed(6, 0, 0);
            }
        }
        private void DrawScreenQuad2(ShaderResourceView srv) {
            var stride = Basic32.Stride;
            const int Offset = 0;

            ImmediateContext.InputAssembler.InputLayout = InputLayouts.Basic32;
            ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_screenQuadVB, stride, Offset));
            ImmediateContext.InputAssembler.SetIndexBuffer(_screenQuadIB, Format.R32_UInt, 0);

            var world = new Matrix {
                M11 = 0.25f,
                M22 = 0.25f,
                M33 = 1.0f,
                M41 = -0.75f,
                M42 = -0.75f,
                M44 = 1.0f
            };

            //var world = Matrix.Identity;
            var tech = Effects.DebugTexFX.ViewArgbTech;
            for (int p = 0; p < tech.Description.PassCount; p++) {
                Effects.DebugTexFX.SetWorldViewProj(world);
                Effects.DebugTexFX.SetTexture(srv);
                tech.GetPassByIndex(p).Apply(ImmediateContext);
                ImmediateContext.DrawIndexed(6, 0, 0);
            }
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
            _texMgr = new TextureManager();
            _texMgr.Init(Device);

            _boxModel = BasicModel.CreateBox(Device, 1, 1, 1);
            _boxModel.Materials[0] = new Material {
                Ambient = new Color4(0.8f, 0.8f, 0.8f),
                Diffuse = new Color4(0.4f, 0.4f, 0.4f),
                Specular = new Color4(16.0f, 0.8f, 0.8f, 0.8f),
                Reflect = Color.Black
            };
            _boxModel.DiffuseMapSRV[0] = _texMgr.CreateTexture("Textures/bricks.dds");
            _boxModel.NormalMapSRV[0] = _texMgr.CreateTexture("Textures/bricks_nmap.dds");

            _gridModel = BasicModel.CreateGrid(Device, 20, 30, 50, 40);
            _gridModel.Materials[0] = new Material {
                Ambient = new Color4(0.7f, 0.7f, 0.7f),
                Diffuse = new Color4(0.6f, 0.6f, 0.6f),
                Specular = new Color4(16.0f, 0.4f, 0.4f, 0.4f),
                Reflect = Color.Black
            };
            _gridModel.DiffuseMapSRV[0] = _texMgr.CreateTexture("Textures/floor.dds");
            _gridModel.NormalMapSRV[0] = _texMgr.CreateTexture("Textures/floor_nmap.dds");


            _sphereModel = BasicModel.CreateSphere(Device, 0.5f, 20, 20);
            _sphereModel.Materials[0] = new Material {
                Ambient = new Color4(0.3f, 0.4f, 0.5f),
                Diffuse = new Color4(0.2f, 0.3f, 0.4f),
                Specular = new Color4(16.0f, 0.9f, 0.9f, 0.9f),
                Reflect = new Color4(0.3f, 0.3f, 0.3f)
            };
            _cylinderModel = BasicModel.CreateCylinder(Device, 0.5f, 0.5f, 3.0f, 15, 15);
            _cylinderModel.Materials[0] = new Material {
                Ambient = new Color4(0.8f, 0.8f, 0.8f),
                Diffuse = new Color4(0.4f, 0.4f, 0.4f),
                Specular = new Color4(32.0f, 1f, 1f, 1f),
                Reflect = Color.Black
            };
            _cylinderModel.DiffuseMapSRV[0] = _texMgr.CreateTexture("Textures/bricks.dds");
            _cylinderModel.NormalMapSRV[0] = _texMgr.CreateTexture("Textures/bricks_nmap.dds");

            for (var i = 0; i < 5; i++) {
                _cylinders[i * 2] = new BasicModelInstance {
                    Model = _cylinderModel,
                    World = Matrix.Translation(-5.0f, 1.5f, -10.0f + i * 5.0f),
                    TexTransform = Matrix.Scaling(1, 2, 1)
                };
                _cylinders[i * 2 + 1] = new BasicModelInstance {
                    Model = _cylinderModel,
                    World = Matrix.Translation(5.0f, 1.5f, -10.0f + i * 5.0f),
                    TexTransform = Matrix.Scaling(1, 2, 1)
                };

                _spheres[i * 2] = new BasicModelInstance {
                    Model = _sphereModel,
                    World = Matrix.Translation(-5.0f, 3.5f, -10.0f + i * 5.0f)
                };
                _spheres[i * 2 + 1] = new BasicModelInstance {
                    Model = _sphereModel,
                    World = Matrix.Translation(5.0f, 3.5f, -10.0f + i * 5.0f)
                };
            }

            _grid = new BasicModelInstance {
                Model = _gridModel,
                TexTransform = Matrix.Scaling(8, 10, 1),
                World = Matrix.Identity
            };

            _box = new BasicModelInstance {
                Model = _boxModel,
                TexTransform = Matrix.Scaling(2, 1, 1),
                World = Matrix.Scaling(3.0f, 1.0f, 3.0f) * Matrix.Translation(0, 0.5f, 0)
            };


        }
        private void BuildSkullGeometryBuffers() {
            _skullModel = BasicModel.LoadFromTxtFile(Device, "Models/skull.txt");
            _skullModel.Materials[0] = new Material {
                Ambient = new Color4(0.5f, 0.5f, 0.5f),
                Diffuse = new Color4(0.2f, 0.2f, 0.2f),
                Specular = new Color4(16.0f, 0.5f, 0.5f, 0.5f),
                Reflect = new Color4(0.3f, 0.3f, 0.3f)
            };


            _skull = new BasicModelInstance {
                Model = _skullModel,
                World = Matrix.Scaling(0.5f, 0.5f, 0.5f) * Matrix.Translation(0, 1.0f, 0)
            };
        }
        private void BuildScreenQuadGeometryBuffers() {
            var quad = GeometryGenerator.CreateFullScreenQuad();

            var verts = quad.Vertices.Select(v => new Basic32(v.Position, v.Normal, v.TexC)).ToList();
            var vbd = new BufferDescription(Basic32.Stride * verts.Count, ResourceUsage.Immutable, BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _screenQuadVB = new Buffer(Device, new DataStream(verts.ToArray(), false, false), vbd);

            var ibd = new BufferDescription(sizeof(int) * quad.Indices.Count, ResourceUsage.Immutable, BindFlags.IndexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _screenQuadIB = new Buffer(Device, new DataStream(quad.Indices.ToArray(), false, false), ibd);
        }

        static void Main() {
            Configuration.EnableObjectTracking = true;
            var app = new SsaoDemo(Process.GetCurrentProcess().Handle);
            if (!app.Init()) {
                return;
            }
            try {
                app.Run();
            } catch (Exception ex) {
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
            }
        }
    }
}
