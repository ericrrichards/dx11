using System;
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

namespace AssimpModel {
    class AssimpModelDemo : D3DApp {

        private TextureManager _texMgr;

        private BasicModel _treeModel;
        private BasicModel _stoneModel;
        private BasicModel _dwarfModel;
        private BasicModelInstance _modelInstance;
        private BasicModelInstance _stoneInstance;
        private BasicModelInstance _dwarfInstance;

        private readonly DirectionalLight[] _dirLights;

        private readonly FpsCamera _camera;
        private Point _lastMousePos;
        private bool _disposed;

        private AssimpModelDemo(IntPtr hInstance)
            : base(hInstance) {
            MainWindowCaption = "Assimp Model Demo";
            _lastMousePos = new Point();
            Enable4XMsaa = true;

            _camera = new FpsCamera {
                Position = new Vector3(0, 2, -15)
            };
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

        }
        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Util.ReleaseCom(ref _treeModel);
                    Util.ReleaseCom(ref _stoneModel);
                    Util.ReleaseCom(ref _dwarfModel);
                    Util.ReleaseCom(ref _texMgr);

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

            _treeModel = new BasicModel(Device, _texMgr, "Models/tree.x", "Textures");

            _modelInstance = new BasicModelInstance (_treeModel) {
                
                World = Matrix.RotationX(MathF.PI / 2)
            };

            _stoneModel = new BasicModel(Device, _texMgr, "Models/stone.x", "Textures");
            _stoneInstance = new BasicModelInstance(_stoneModel) {
                World = Matrix.Scaling(0.1f, 0.1f, 0.1f) * Matrix.Translation(2, 0, 2)
            };

            _dwarfModel = new BasicModel(Device, _texMgr, "Models/Bob.md5mesh", "Textures", true);
            for (int i = 0; i < _dwarfModel.Materials.Count; i++) {
                _dwarfModel.Materials[i] = new Material() {
                    Ambient = Color.DarkGray,
                    Diffuse = Color.White,
                    Specular = new Color4(128, 1f, 1.0f, 1.0f)
                };
            }
            
            _dwarfInstance = new BasicModelInstance(_dwarfModel) {
                World = Matrix.RotationX(-MathF.PI / 2)* Matrix.Scaling(0.05f, 0.05f, 0.05f)*Matrix.Translation(4, 0, 4)
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


        }
        public override void DrawScene() {
            base.DrawScene();
            ImmediateContext.ClearRenderTargetView(RenderTargetView, Color.Silver);
            ImmediateContext.ClearDepthStencilView(
                DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);

            ImmediateContext.InputAssembler.InputLayout = InputLayouts.PosNormalTexTan;
            ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            _camera.UpdateViewMatrix();
            var viewProj = _camera.ViewProj;
            var view = _camera.View;
            var proj = _camera.Proj;

            Effects.NormalMapFX.SetDirLights(_dirLights);
            Effects.NormalMapFX.SetEyePosW(_camera.Position);

            var activeTech = Effects.NormalMapFX.Light3TexTech;
            for (var p = 0; p < activeTech.Description.PassCount; p++) {
                var pass = activeTech.GetPassByIndex(p);
                _modelInstance.Draw(ImmediateContext, pass, view,proj);
                _stoneInstance.Draw(ImmediateContext, pass, view,proj);
                _dwarfInstance.Draw(ImmediateContext, pass, view, proj);
            }

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

        static void Main() {
            Configuration.EnableObjectTracking = true;
            var app = new AssimpModelDemo(Process.GetCurrentProcess().Handle);
            if (!app.Init()) {
                return;
            }
            app.Run();
        }
    }
}
