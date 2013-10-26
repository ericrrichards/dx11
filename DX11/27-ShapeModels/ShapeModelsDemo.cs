using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _27_ShapeModels {
    using System.Diagnostics;
    using System.Drawing;
    using System.Windows.Forms;

    using Core;
    using Core.Camera;
    using Core.FX;
    using Core.Model;

    using SlimDX;
    using SlimDX.DXGI;
    using SlimDX.Direct3D11;

    class ShapeModelsDemo : D3DApp {

        private TextureManager _texMgr;

        private readonly DirectionalLight[] _dirLights;

        private readonly FpsCamera _camera;

        private BasicModel _boxModel;
        private BasicModel _gridModel;
        private BasicModel _sphereModel;
        private BasicModel _cylinderModel;

        private BasicModelInstance _grid;
        private BasicModelInstance _box;
        private BasicModelInstance _sphere;
        private BasicModelInstance _cylinder;

        private Point _lastMousePos;
        private bool _disposed;

        protected ShapeModelsDemo(IntPtr hInstance) : base(hInstance) {
            MainWindowCaption = "ShapeModels Demo";

            _lastMousePos = new Point();

            _camera = new FpsCamera { Position = new Vector3(0, 2, -15) };

            _dirLights = new[] {
                new DirectionalLight {
                    Ambient = new Color4( 0.2f, 0.2f, 0.2f),
                    Diffuse = new Color4(0.7f, 0.7f, 0.7f),
                    Specular = new Color4(0.8f, 0.8f, 0.8f),
                    Direction = new Vector3(-0.57735f, -0.57735f, 0.57735f)
                },
                new DirectionalLight {
                    Ambient = new Color4(0,0,0),
                    Diffuse = new Color4(1.0f, 0.4f, 0.4f, 0.4f),
                    Specular = new Color4(1.0f, 0.2f, 0.2f, 0.2f),
                    Direction = new Vector3(-0.707f, -0.707f, 0)
                },
                new DirectionalLight {
                    Ambient = new Color4(0,0,0),
                    Diffuse = new Color4(1.0f,0.2f, 0.2f, 0.2f),
                    Specular = new Color4(0.2f,0.2f,0.2f),
                    Direction = new Vector3(0, 0, -1)
                }
            };
        }
        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Util.ReleaseCom(ref _texMgr);

                    Util.ReleaseCom(ref _gridModel);
                    Util.ReleaseCom(ref _boxModel);
                    Util.ReleaseCom(ref _sphereModel);
                    Util.ReleaseCom(ref _cylinderModel);
                    
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

            _texMgr = new TextureManager();
            _texMgr.Init(Device);


            _gridModel = BasicModel.CreateGrid(Device, 20, 20, 40, 40);
            _gridModel.Materials[0] = new Material() { Diffuse = Color.SaddleBrown, Specular = new Color4(16, .9f, .9f, .9f)};
            _gridModel.DiffuseMapSRV[0] = _texMgr.CreateTexture("Textures/floor.dds");
            _gridModel.NormalMapSRV[0] = _texMgr.CreateTexture("textures/floor_nmap.dds");
            
            _boxModel = BasicModel.CreateBox(Device, 1, 1, 1);
            _boxModel.Materials[0] = new Material() { Ambient = Color.Red, Diffuse = Color.Red, Specular = new Color4(64.0f, 1.0f, 1.0f, 1.0f)};
            _boxModel.NormalMapSRV[0] = _texMgr.CreateTexture("Textures/bricks_nmap.dds");

            _sphereModel = BasicModel.CreateSphere(Device, 1, 20, 20);
            _sphereModel.Materials[0] = new Material() { Ambient = Color.Blue, Diffuse = Color.Blue, Specular = new Color4(64.0f, 1.0f, 1.0f, 1.0f)};
            _sphereModel.NormalMapSRV[0] = _texMgr.CreateTexture("Textures/stones_nmap.dds");

            _cylinderModel = BasicModel.CreateCylinder(Device, 1, 1, 3, 20, 20);
            _cylinderModel.Materials[0] = new Material() { Ambient = Color.Green, Diffuse = Color.Green, Specular = new Color4(64.0f, 1.0f, 1.0f, 1.0f)};
            _cylinderModel.NormalMapSRV[0] = _texMgr.CreateTexture("Textures/stones_nmap.dds");

            _grid = new BasicModelInstance() {
                Model = _gridModel, TexTransform = Matrix.Scaling(10, 10, 1),
                World = Matrix.Identity
            };

            _box = new BasicModelInstance() {
                Model = _boxModel,
                World = Matrix.Translation(-3, 1, 0)
            };

            _sphere = new BasicModelInstance() {
                Model = _sphereModel,
                World = Matrix.Translation(0, 1, 0)
            };

            _cylinder = new BasicModelInstance() {
                Model = _cylinderModel,
                World = Matrix.Translation(3, 1.5f, 0)
            };
            
            return true;
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

            _camera.UpdateViewMatrix();

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

public override void DrawScene() {
    ImmediateContext.ClearRenderTargetView(RenderTargetView, Color.Silver);
    ImmediateContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);

    var viewProj = _camera.ViewProj;

    Effects.NormalMapFX.SetDirLights(_dirLights);
    Effects.NormalMapFX.SetEyePosW(_camera.Position);

    var floorTech = Effects.NormalMapFX.Light3TexTech;
    var activeTech = Effects.NormalMapFX.Light3Tech;

    ImmediateContext.InputAssembler.InputLayout = InputLayouts.PosNormalTexTan;
    ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

    if (Util.IsKeyDown(Keys.W)) {
        ImmediateContext.Rasterizer.State = RenderStates.WireframeRS;
    }

    for (int p = 0; p < activeTech.Description.PassCount; p++) {
        var pass = activeTech.GetPassByIndex(p);
        _box.Draw(ImmediateContext, pass, viewProj);
        _sphere.Draw(ImmediateContext, pass, viewProj);
        _cylinder.Draw(ImmediateContext, pass, viewProj);
    }
    for (int p = 0; p < floorTech.Description.PassCount; p++) {
        var pass = activeTech.GetPassByIndex(p);
        _grid.Draw(ImmediateContext, pass, viewProj);
    }
    SwapChain.Present(0, PresentFlags.None);
    ImmediateContext.Rasterizer.State = null;
}

        static void Main(string[] args) {
            Configuration.EnableObjectTracking = true;
            var app = new ShapeModelsDemo(Process.GetCurrentProcess().Handle);
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
