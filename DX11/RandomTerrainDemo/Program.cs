using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Core;
using Core.Camera;
using Core.FX;
using Core.Terrain;
using SlimDX;
using SlimDX.DXGI;
using SlimDX.Direct3D11;

namespace RandomTerrainDemo {
    using Buffer = SlimDX.Direct3D11.Buffer;
    class RandomTerrainDemo : D3DApp {
        private Sky _sky;
        private Terrain _terrain;
        private readonly DirectionalLight[] _dirLights;

        private readonly LookAtCamera _camera;

        private bool _camWalkMode;
        private Point _lastMousePos;
        private bool _disposed;

        #region UI Elements
        private Panel _panel;
        private Button _generateButton;
        #endregion


        protected RandomTerrainDemo(IntPtr hInstance) : base(hInstance) {
            MainWindowCaption = "Random Terrain Demo";
            //Enable4xMsaa = true;
            _lastMousePos = new Point();

            _camera = new LookAtCamera {
                Position = new Vector3(0, 20, 100)
            };
            _dirLights = new[] {
                new DirectionalLight {
                    Ambient = new Color4( 0.3f, 0.3f, 0.3f),
                    Diffuse = new Color4(1.0f, 1.0f, 1.0f),
                    Specular = new Color4(0.8f, 0.8f, 0.7f),
                    Direction = new Vector3(.707f, -0.707f, 0.0f)
                },
                new DirectionalLight {
                    Ambient = new Color4(0,0,0),
                    Diffuse = new Color4(1.0f, 0.2f, 0.2f, 0.2f),
                    Specular = new Color4(1.0f, 0.2f, 0.2f, 0.2f),
                    Direction = new Vector3(0.57735f, -0.57735f, 0.57735f)
                },
                new DirectionalLight {
                    Ambient = new Color4(0,0,0),
                    Diffuse = new Color4(0.2f, 0.2f, 0.2f),
                    Specular = new Color4(0.2f,0.2f,0.2f),
                    Direction = new Vector3(-0.57735f, -0.57735f, -0.57735f)
                }
            };
        }
        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    ImmediateContext.ClearState();
                    Util.ReleaseCom(ref _sky);
                    Util.ReleaseCom(ref _terrain);
                    

                    Effects.DestroyAll();
                    InputLayouts.DestroyAll();
                    RenderStates.DestroyAll();
                    Patch.DestroyPatchIndexBuffers();
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
            Patch.InitPatchData(Terrain.CellsPerPatch, Device);

            _sky = new Sky(Device, "Textures/grasscube1024.dds", 5000.0f);

            var tii = new InitInfo {
                HeightMapFilename = null,
                LayerMapFilename0 = "textures/grass.dds",
                LayerMapFilename1 = "textures/darkdirt.dds",
                LayerMapFilename2 = "textures/stone.dds",
                LayerMapFilename3 = "Textures/lightdirt.dds",
                LayerMapFilename4 = "textures/snow.dds",
                BlendMapFilename = null,
                HeightScale = 50.0f,
                HeightMapWidth = 2049,
                HeightMapHeight = 2049,
                CellSpacing = 0.5f,

                Seed = 0,
                NoiseSize1 = 1.0f,
                Persistence1 = 0.7f,
                Octaves1 = 7,
                NoiseSize2 = 2.5f, 
                Persistence2 = 0.8f,
                Octaves2 = 3
            };
            _terrain = new Terrain();
            _terrain.Init(Device, ImmediateContext, tii);

            _camera.Height = _terrain.Height;

            AddUIElements();

            return true;
        }

        private void AddUIElements() {
            _panel = new Panel {
                Dock = DockStyle.Top,
                AutoSize = true
            };

            _generateButton = new Button {
                Text = "Generate Terrain",
                AutoSize = true
            };
            _generateButton.Click += (sender, args) => {
                Window.Cursor = Cursors.WaitCursor;
                Util.ReleaseCom(ref _terrain);
                _terrain = new Terrain();
                var tii = new InitInfo {
                    HeightMapFilename = null,
                    LayerMapFilename0 = "textures/grass.dds",
                    LayerMapFilename1 = "textures/darkdirt.dds",
                    LayerMapFilename2 = "textures/stone.dds",
                    LayerMapFilename3 = "Textures/lightdirt.dds",
                    LayerMapFilename4 = "textures/snow.dds",
                    BlendMapFilename = null,
                    HeightScale = 50.0f,
                    HeightMapWidth = 2049,
                    HeightMapHeight = 2049,
                    CellSpacing = 0.5f,

                    Seed = 0,
                    NoiseSize1 = 1.0f,
                    Persistence1 = 0.7f,
                    Octaves1 = 7,
                    NoiseSize2 = 2.5f,
                    Persistence2 = 0.8f,
                    Octaves2 = 3
                };
                _terrain.Init(Device, ImmediateContext, tii);
                _camera.Height = _terrain.Height;
                Window.Cursor = Cursors.Default;
            };
            
            _panel.Controls.Add(_generateButton);
            Window.Controls.Add(_panel);

        }

        public override void OnResize() {
            base.OnResize();
            _camera.SetLens(0.25f * MathF.PI, AspectRatio, 1.0f, 1000.0f);
        }

        public override void UpdateScene(float dt) {
            base.UpdateScene(dt);
            if (Util.IsKeyDown(Keys.Up)) {
                _camera.Walk(10.0f*dt);
            }
            if (Util.IsKeyDown(Keys.Down)) {
                _camera.Walk(-10.0f*dt);
            }
            if (Util.IsKeyDown(Keys.Left)) {
                _camera.Strafe(-10.0f*dt);
            }
            if (Util.IsKeyDown(Keys.Right)) {
                _camera.Strafe(10.0f*dt);
            }
            if (Util.IsKeyDown(Keys.PageUp)) {
                _camera.Zoom(-dt*10.0f);
            }
            if (Util.IsKeyDown(Keys.PageDown)) {
                _camera.Zoom(+dt*10.0f);
            }
            if (Util.IsKeyDown(Keys.D2)) {
                _camWalkMode = true;
            }
            if (Util.IsKeyDown(Keys.D3)) {
                _camWalkMode = false;
            }
            if (_camWalkMode) {
                var camPos = _camera.Target;
                var y = _terrain.Height(camPos.X, camPos.Z);
                _camera.Target = new Vector3(camPos.X, y, camPos.Z);

            }
            _camera.UpdateViewMatrix();
        }
        public override void DrawScene() {
            ImmediateContext.ClearRenderTargetView(RenderTargetView, Color.Silver);
            ImmediateContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);

            if (Util.IsKeyDown(Keys.W)) {
                ImmediateContext.Rasterizer.State = RenderStates.WireframeRS;
            }
            _terrain.Draw(ImmediateContext, _camera, _dirLights);
            
            ImmediateContext.Rasterizer.State = null;
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


        static void Main(string[] args) {
            Configuration.EnableObjectTracking = true;
            var app = new RandomTerrainDemo(Process.GetCurrentProcess().Handle);
            if (!app.Init()) {
                return;
            }
            app.Run();
        }
    }
}
