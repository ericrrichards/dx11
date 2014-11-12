using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Core;
using Core.Camera;
using Core.FX;
using Core.Model;
using SlimDX;
using SlimDX.Direct3D11;
using PresentFlags = SlimDX.DXGI.PresentFlags;

namespace HemisphericalAmbient {
    class HemisphericalAmbientDemo : D3DApp {
        private TextureManager _texMgr;
        private BasicModel _bunnyModel;
        private BasicModelInstance _bunnyInstance;

        private readonly FpsCamera _camera;
        private Point _lastMousePos;
        private bool _disposed;

        private ForwardLightingEffect _effect;
        private Vector3 _ambientLower = new Vector3(0.1f, 0.5f, 0.1f);
        private Vector3 _ambientUpper = new Vector3(0.1f, 0.2f, 0.5f);

        private Label _lblLower;
        private Label _lblUpper;
        private Label _lblRed;
        private Label _lblGreen;
        private Label _lblBlue;

        private HScrollBar _hsbLowerR;
        private HScrollBar _hsbLowerG;
        private HScrollBar _hsbLowerB;
        private HScrollBar _hsbUpperR;
        private HScrollBar _hsbUpperG;
        private HScrollBar _hsbUpperB;

        private TableLayoutPanel _tblLayout;


        private void AddUIElements() {
            var toolTip = new ToolTip();
            toolTip.AutoPopDelay = 5000;
            toolTip.InitialDelay = 1000;
            toolTip.ReshowDelay = 500;
            toolTip.ShowAlways = true;

            _hsbLowerR = new HScrollBar {
                Minimum = 0,
                Maximum = 109,
                LargeChange = 10,
                SmallChange = 1,
                Value = (int)(_ambientLower.X * 100),
                Dock = DockStyle.Fill
            };
            _hsbLowerR.ValueChanged += (sender, args) => {
                _ambientLower.X = _hsbLowerR.Value / 100.0f;
                toolTip.SetToolTip(_hsbLowerR, _ambientLower.X.ToString(CultureInfo.InvariantCulture));
            };
            _hsbLowerG = new HScrollBar {
                Minimum = 0,
                Maximum = 109,
                LargeChange = 10,
                SmallChange = 1,
                Value = (int)(_ambientLower.Y * 100),
                Dock = DockStyle.Fill
            };
            _hsbLowerG.ValueChanged += (sender, args) => {
                _ambientLower.Y = _hsbLowerG.Value / 100.0f;
                toolTip.SetToolTip(_hsbLowerG, _ambientLower.Y.ToString(CultureInfo.InvariantCulture));
            };
            _hsbLowerB = new HScrollBar {
                Minimum = 0,
                Maximum = 109,
                LargeChange = 10,
                SmallChange = 1,
                Value = (int)(_ambientLower.Z * 100),
                Dock = DockStyle.Fill
            };
            _hsbLowerB.ValueChanged += (sender, args) => {
                _ambientLower.Z = _hsbLowerB.Value / 100.0f;
                toolTip.SetToolTip(_hsbLowerB, _ambientLower.Z.ToString(CultureInfo.InvariantCulture));
            };
            _hsbUpperR = new HScrollBar {
                Minimum = 0,
                Maximum = 109,
                LargeChange = 10,
                SmallChange = 1,
                Value = (int)(_ambientUpper.X * 100),
                Dock = DockStyle.Fill
            };
            _hsbUpperR.ValueChanged += (sender, args) => {
                _ambientUpper.X = _hsbUpperR.Value / 100.0f;
                toolTip.SetToolTip(_hsbUpperR, _ambientUpper.X.ToString(CultureInfo.InvariantCulture));
            };
            _hsbUpperG = new HScrollBar {
                Minimum = 0,
                Maximum = 109,
                LargeChange = 10,
                SmallChange = 1,
                Value = (int)(_ambientUpper.Y * 100),
                Dock = DockStyle.Fill
            };
            _hsbUpperG.ValueChanged += (sender, args) => {
                _ambientUpper.Y = _hsbUpperG.Value / 100.0f;
                toolTip.SetToolTip(_hsbUpperG, _ambientUpper.Y.ToString(CultureInfo.InvariantCulture));
            };
            _hsbUpperB = new HScrollBar {
                Minimum = 0,
                Maximum = 109,
                LargeChange = 10,
                SmallChange = 1,
                Value = (int)(_ambientUpper.Z * 100),
                Dock = DockStyle.Fill
            };
            _hsbUpperB.ValueChanged += (sender, args) => {
                _ambientUpper.Z = _hsbUpperB.Value / 100.0f;
                toolTip.SetToolTip(_hsbUpperB, _ambientUpper.Z.ToString(CultureInfo.InvariantCulture));
            };

            _lblUpper = new Label {
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Text = "Upper Color"
            };
            _lblLower = new Label {
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Text = "Lower Color"
            };
            _lblRed = new Label {
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Text = "Red",
            };
            _lblGreen = new Label {
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Text = "Green"
            };
            _lblBlue = new Label {
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Text = "Blue"
            };


            _tblLayout = new TableLayoutPanel {
                Dock = DockStyle.Top,
                AutoSize = true
            };
            _tblLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _tblLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _tblLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _tblLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent) { Width = .25f });
            _tblLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent) { Width = .25f });
            _tblLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent) { Width = .25f });
            _tblLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent) { Width = .25f });

            _tblLayout.Controls.Add(_lblUpper, 0, 1);
            _tblLayout.Controls.Add(_lblLower, 0, 2);
            _tblLayout.Controls.Add(_lblRed, 1, 0);
            _tblLayout.Controls.Add(_lblGreen, 2, 0);
            _tblLayout.Controls.Add(_lblBlue, 3, 0);

            _tblLayout.Controls.Add(_hsbUpperR, 1, 1);
            _tblLayout.Controls.Add(_hsbUpperG, 2, 1);
            _tblLayout.Controls.Add(_hsbUpperB, 3, 1);
            _tblLayout.Controls.Add(_hsbLowerR, 1, 2);
            _tblLayout.Controls.Add(_hsbLowerG, 2, 2);
            _tblLayout.Controls.Add(_hsbLowerB, 3, 2);


            Window.Controls.Add(_tblLayout);
        }

        private Vector3 GammaToLinear(Vector3 c) {
            return new Vector3(c.X * c.X, c.Y * c.Y, c.Z * c.Z);
        }

        private HemisphericalAmbientDemo(IntPtr hInstance)
            : base(hInstance) {
            MainWindowCaption = "Hemispherical Ambient Light Demo";
            _lastMousePos = new Point();
            //Enable4XMsaa = true;

            _camera = new FpsCamera {
                Position = new Vector3(0, 2, -15)
            };

        }

        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Util.ReleaseCom(ref _bunnyModel);
                    Util.ReleaseCom(ref _texMgr);

                    Effects.DestroyAll();
                    InputLayouts.DestroyAll();
                    RenderStates.DestroyAll();

                    Util.ReleaseCom(ref _effect);
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

            _bunnyModel = BasicModel.LoadSdkMesh(Device, _texMgr, "Models/bunny.sdkmesh", "Textures");

            _bunnyInstance = new BasicModelInstance(_bunnyModel) {
                World = Matrix.Scaling(0.1f, 0.1f, 0.1f)
            };

            _effect = new ForwardLightingEffect(Device, "FX/forwardLight.fxo");

            AddUIElements();

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
            ImmediateContext.ClearRenderTargetView(RenderTargetView, Color.Black);
            ImmediateContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);

            ImmediateContext.InputAssembler.InputLayout = InputLayouts.PosNormalTexTan;
            ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            _camera.UpdateViewMatrix();
            
            var view = _camera.View;
            var proj = _camera.Proj;

            SetupAmbient();

            var activeTech = _effect.Ambient;
            for (var p = 0; p < activeTech.Description.PassCount; p++) {
                var pass = activeTech.GetPassByIndex(p);
                _bunnyInstance.Draw(ImmediateContext, pass, view, proj, DrawAmbient);
            }

            SwapChain.Present(0, PresentFlags.None);
        }

        private void SetupAmbient() {
            var lowerGamma = GammaToLinear(_ambientLower);
            var upperGamma = GammaToLinear(_ambientUpper);
            var range = upperGamma - lowerGamma;
            _effect.SetAmbientDown(lowerGamma);
            _effect.SetAmbientRange(range);
        }

        private void DrawAmbient(DeviceContext dc, EffectPass pass, Matrix view, Matrix proj) {
            var model = _bunnyInstance.Model;
            var world = _bunnyInstance.World;

            _effect.SetWorld(world);
            _effect.SetWorldViewProj(world * view * proj);

            for (var i = 0; i < model.SubsetCount; i++) {
                _effect.SetDiffuseMap(model.DiffuseMapSRV[i]);
                pass.Apply(ImmediateContext);
                model.ModelMesh.Draw(ImmediateContext, i);
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

        static void Main() {
            Configuration.EnableObjectTracking = true;
            var app = new HemisphericalAmbientDemo(Process.GetCurrentProcess().Handle);
            if (!app.Init()) {
                return;
            }
            app.Run();
        }
    }
}
