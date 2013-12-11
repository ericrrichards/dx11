using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Core;
using Core.Camera;
using Core.FX;
using Core.Terrain;
using SlimDX;
using SlimDX.DXGI;
using SlimDX.Direct3D11;

namespace ParticlesDemo {
    class ParticlesDemo : D3DApp {
        private Sky _sky;
        private Terrain _terrain;
        private ShaderResourceView _flareTexSRV;
        private ShaderResourceView _rainTexSRV;
        private ShaderResourceView _randomTex;

        private ParticleSystem _fire;
        private ParticleSystem _rain;

        private readonly DirectionalLight[] _dirLights;

        private readonly FpsCamera _camera;
        private Point _lastMousePos;

        private bool _disposed;
        private bool _camWalkMode;

        private ParticlesDemo(IntPtr hInstance) : base(hInstance) {
            MainWindowCaption = "Particles Demo";

            _camera = new FpsCamera {
                Position = new Vector3(0, 2, 100)
            };
            _dirLights = new[] {
                new DirectionalLight {
                    Ambient = new Color4( 0.3f, 0.3f, 0.3f),
                    Diffuse = new Color4(1f, 1f, 1f),
                    Specular = new Color4(0.8f, 0.8f, 0.8f),
                    Direction = new Vector3(0, -0.707f, -0.707f)//
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
                    Specular = new Color4(0.2f, 0.2f, 0.2f),
                    Direction = new Vector3(-0.57735f, -0.57735f, 0.57735f)
                }
            };
        }
        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    ImmediateContext.ClearState();

                    Util.ReleaseCom(ref _randomTex);
                    Util.ReleaseCom(ref _flareTexSRV);
                    Util.ReleaseCom(ref _rainTexSRV);

                    Util.ReleaseCom(ref _sky);
                    Util.ReleaseCom(ref _terrain);
                    Patch.DestroyPatchIndexBuffers();

                    Util.ReleaseCom(ref _rain);
                    Util.ReleaseCom(ref _fire);

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
            Enable4XMsaa = true;
            Effects.InitAll(Device);
            InputLayouts.InitAll(Device);
            RenderStates.InitAll(Device);

            _sky = new Sky(Device, "Textures/grasscube1024.dds", 5000.0f);

            var tii = new InitInfo {
                HeightMapFilename = "Textures/terrain.raw",
                LayerMapFilename0 = "textures/grass.dds",
                LayerMapFilename1 = "textures/darkdirt.dds",
                LayerMapFilename2 = "textures/stone.dds",
                LayerMapFilename3 = "Textures/lightdirt.dds",
                LayerMapFilename4 = "textures/snow.dds",
                BlendMapFilename = "textures/blend.dds",
                HeightScale = 50.0f,
                HeightMapWidth = 2049,
                HeightMapHeight = 2049,
                CellSpacing = 0.5f
            };
            Patch.InitPatchData(Terrain.CellsPerPatch, Device);
            _terrain = new Terrain();
            _terrain.Init(Device, ImmediateContext, tii);

            _randomTex = Util.CreateRandomTexture1DSRV(Device);

            _flareTexSRV = Util.CreateTexture2DArraySRV(Device, ImmediateContext, new[] {"Textures/flare0.dds"}, Format.R8G8B8A8_UNorm);
            _fire = new ParticleSystem();
            _fire.Init(Device, Effects.FireFX, _flareTexSRV, _randomTex, 500);
            _fire.EmitPosW = new Vector3(0, 1.0f, 120.0f);

            _rainTexSRV = Util.CreateTexture2DArraySRV(Device, ImmediateContext, new[] {"Textures/raindrop.dds"}, Format.R8G8B8A8_UNorm);
            _rain = new ParticleSystem();
            _rain.Init(Device, Effects.RainFX, _rainTexSRV, _randomTex, 10000);

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
                _camera.Zoom(-dt * 10.0f);
            }
            if (Util.IsKeyDown(Keys.PageDown)) {
                _camera.Zoom(+dt * 10.0f);
            }
            if (Util.IsKeyDown(Keys.D1)) {
                _camWalkMode = true;
            }
            if (Util.IsKeyDown(Keys.D2)) {
                _camWalkMode = false;
            }
            if (_camWalkMode) {
                var camPos = _camera.Position;
                var y = _terrain.Height(camPos.X, camPos.Z);
                _camera.Position = new Vector3(camPos.X, y+2.0f, camPos.Z);

            }

            if (Util.IsKeyDown(Keys.R)) {
                _fire.Reset();
                _rain.Reset();
            }

            _fire.Update(dt, Timer.TotalTime);
            _rain.Update(dt, Timer.TotalTime);
            
            _camera.UpdateViewMatrix();
        }
        public override void DrawScene() {
            ImmediateContext.ClearRenderTargetView(RenderTargetView, Color.Silver);
            ImmediateContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth|DepthStencilClearFlags.Stencil, 1.0f, 0 );

            ImmediateContext.InputAssembler.InputLayout = InputLayouts.Basic32;
            ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            if (Util.IsKeyDown(Keys.W)) {
                ImmediateContext.Rasterizer.State = RenderStates.WireframeRS;
            }
            _terrain.Renderer.Draw(ImmediateContext, _camera, _dirLights);
            ImmediateContext.Rasterizer.State = null;

            _sky.Draw(ImmediateContext, _camera);

            _fire.EyePosW = _camera.Position;
            _fire.Draw(ImmediateContext, _camera);

            ImmediateContext.OutputMerger.BlendState = null;
            var blendFactor = new Color4(0,0,0,0);
            ImmediateContext.OutputMerger.BlendFactor = blendFactor;
            ImmediateContext.OutputMerger.BlendSampleMask = -1;

            _rain.EyePosW = _camera.Position;
            _rain.EmitPosW = _camera.Position;

            _rain.Draw(ImmediateContext, _camera);

            ImmediateContext.Rasterizer.State = null;
            ImmediateContext.OutputMerger.BlendState = null;
            
            ImmediateContext.OutputMerger.BlendFactor = blendFactor;
            ImmediateContext.OutputMerger.BlendSampleMask = -1;

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

        static void Main() {
            Configuration.EnableObjectTracking = true;
            var app = new ParticlesDemo(Process.GetCurrentProcess().Handle);
            if (!app.Init()) {
                return;
            }
            app.Run();
        }
    }
}
