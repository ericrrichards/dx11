using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Core;
using Core.Camera;
using Core.FX;
using Core.Model;
using SlimDX;
using SlimDX.Direct3D11;
using SlimDX.DXGI;

namespace _36_Geosphere {
    class GeosphereDemo :D3DApp {
        private TextureManager _texMgr;

        private readonly DirectionalLight[] _dirLights;

        private readonly LookAtCamera _camera;

        private BasicModel _geosphereModel;
        private BasicModelInstance _geosphere;

        private BasicModel _sphereModel;
        private BasicModelInstance _sphere;
        private bool _showSphere;

        private GeometryGenerator.SubdivisionCount _subdivisions = 0;

        private Point _lastMousePos;
        private bool _disposed;

        private GeosphereDemo(IntPtr hInstance) : base(hInstance) {
            MainWindowCaption = "Geosphere Demo";

            _lastMousePos = new Point();

            _camera = new LookAtCamera();
            _camera.LookAt(new Vector3(0, 2, -15), new Vector3(), Vector3.UnitY);

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

                    Util.ReleaseCom(ref _geosphereModel);
                    Util.ReleaseCom(ref _sphereModel);

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


            _geosphereModel = new BasicModel();
            _geosphereModel.CreateGeosphere(Device, 5, _subdivisions);

            _sphereModel = new BasicModel();
            _sphereModel.CreateSphere(Device, 5, 20,20);
            
            _geosphere = new BasicModelInstance(_geosphereModel) {
                World = Matrix.Identity
            };
            _sphere = new BasicModelInstance(_sphereModel);


            return true;
        }
        public override void OnResize() {
            base.OnResize();
            _camera.SetLens(0.25f * MathF.PI, AspectRatio, 1.0f, 1000.0f);
        }

        private float _inputDelay = 0;
        public override void UpdateScene(float dt) {
            base.UpdateScene(dt);
            _inputDelay -= dt;
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

            if (Util.IsKeyDown(Keys.D0)) {
                _subdivisions = GeometryGenerator.SubdivisionCount.None;
                RebuildGeosphere();
            }
            if (Util.IsKeyDown(Keys.D1)) {
                _subdivisions = GeometryGenerator.SubdivisionCount.One;
                RebuildGeosphere();
            }
            if (Util.IsKeyDown(Keys.D2)) {
                _subdivisions = GeometryGenerator.SubdivisionCount.Two;
                RebuildGeosphere();
            }
            if (Util.IsKeyDown(Keys.D3)) {
                _subdivisions = GeometryGenerator.SubdivisionCount.Three;
                RebuildGeosphere();
            }
            if (Util.IsKeyDown(Keys.D4)) {
                _subdivisions = GeometryGenerator.SubdivisionCount.Four;
                RebuildGeosphere();
            }
            if (Util.IsKeyDown(Keys.D5)) {
                _subdivisions = GeometryGenerator.SubdivisionCount.Five;
                RebuildGeosphere();
            }
            if (Util.IsKeyDown(Keys.D6)) {
                _subdivisions = GeometryGenerator.SubdivisionCount.Six;
                RebuildGeosphere();
            }
            if (Util.IsKeyDown(Keys.D7)) {
                _subdivisions = GeometryGenerator.SubdivisionCount.Seven;
                RebuildGeosphere();
            }
            if (Util.IsKeyDown(Keys.D8)) {
                _subdivisions = GeometryGenerator.SubdivisionCount.Eight;
                RebuildGeosphere();
            }

            if (Util.IsKeyDown(Keys.S)) {
                _showSphere = true;
            }
            if (Util.IsKeyDown(Keys.G)) {
                _showSphere = false;
            }
            
            
            _camera.UpdateViewMatrix();

        }

        private void RebuildGeosphere() {
            if (_inputDelay < 0) {
                Util.ReleaseCom(ref _geosphereModel);
                _geosphereModel = new BasicModel();
                _geosphereModel.CreateGeosphere(Device, 5, _subdivisions);
                _geosphere.Model = _geosphereModel;
                _inputDelay = 1;
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
        public override void DrawScene() {
            ImmediateContext.ClearRenderTargetView(RenderTargetView, Color.Silver);
            ImmediateContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);

            var view = _camera.View;
            var proj = _camera.Proj;

            Effects.BasicFX.SetDirLights(_dirLights);
            Effects.BasicFX.SetEyePosW(_camera.Position);

            var activeTech = Effects.BasicFX.Light3Tech;

            ImmediateContext.InputAssembler.InputLayout = InputLayouts.PosNormalTexTan;
            ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            if (Util.IsKeyDown(Keys.W)) {
                ImmediateContext.Rasterizer.State = RenderStates.WireframeRS;
            }

            for (var p = 0; p < activeTech.Description.PassCount; p++) {
                var pass = activeTech.GetPassByIndex(p);
                if (_showSphere) {
                    _sphere.Draw(ImmediateContext, pass, view, proj, RenderMode.Basic);
                } else {
                    _geosphere.Draw(ImmediateContext, pass, view, proj, RenderMode.Basic);
                }
            }

            SwapChain.Present(0, PresentFlags.None);
            ImmediateContext.Rasterizer.State = null;
        }


        static void Main(string[] args) {
            Configuration.EnableObjectTracking = true;
            var app = new GeosphereDemo(Process.GetCurrentProcess().Handle);
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
