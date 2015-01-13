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

namespace PointLighting {
    partial class PointLightDemo :D3DApp{

        private TextureManager _texMgr;
        private BasicModel _bunnyModel;
        private BasicModelInstance _bunnyInstance;

        private readonly FpsCamera _camera;
        private Point _lastMousePos;
        private bool _disposed;

        private FowardLightingEffect _effect;
        private InputLayout _layout;
        private BlendState _additiveBlend;

        private Vector3 _ambientLowerColor = new Vector3(0.1f, 0.2f, 0.1f);
        private Vector3 _ambientUpperColor = new Vector3(0.1f, 0.2f, 0.2f);
        private Vector3 _dirLightColor = new Vector3(0.0f, 0.0f, 0.0f);
        private Vector3 _dirLightDirection = new Vector3(0.0f, -1.0f, 0.0f);

        private const int NumLights = 3;
        Vector3[] _lightPositions = new Vector3[NumLights] {
            new Vector3(25.0f, 13.0f, 14.4f),
            new Vector3(-25.0f, 13.0f, 14.4f),
            new Vector3(0, 13.0f, -28.9f)
        };
        Vector3[] _lightColor = new Vector3[NumLights] {
            Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ 
        };
        private float _lightRange = 50.0f;


        private PointLightDemo(IntPtr hInstance) : base(hInstance) {
            MainWindowCaption = "Point Light Demo";
            _lastMousePos = new Point();
            Enable4XMsaa = true;
            GammaCorrectedBackBuffer = true;

            _camera = new FpsCamera();
            _camera.LookAt(new Vector3(71, 41, 71), Vector3.Zero, Vector3.UnitY);

            _dirLightDirection.Normalize();
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
                    Util.ReleaseCom(ref _layout);
                    Util.ReleaseCom(ref _additiveBlend);
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

            _bunnyInstance = new BasicModelInstance(_bunnyModel);

            _effect = new FowardLightingEffect(Device, "FX/ForwardLight.fxo");

            var blendDesc = new BlendStateDescription {
                AlphaToCoverageEnable = false,
                IndependentBlendEnable = false
            };
            blendDesc.RenderTargets[0].BlendEnable = true;
            blendDesc.RenderTargets[0].SourceBlend = BlendOption.One;
            blendDesc.RenderTargets[0].DestinationBlend = BlendOption.One;
            blendDesc.RenderTargets[0].BlendOperation = BlendOperation.Add;
            blendDesc.RenderTargets[0].SourceBlendAlpha = BlendOption.One;
            blendDesc.RenderTargets[0].DestinationBlendAlpha = BlendOption.One;
            blendDesc.RenderTargets[0].BlendOperationAlpha = BlendOperation.Add;
            blendDesc.RenderTargets[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;

            _additiveBlend = BlendState.FromDescription(Device, blendDesc);


            var passDesc = _effect.Ambient.GetPassByIndex(0).Description;
            _layout = new InputLayout(Device, passDesc.Signature, InputLayoutDescriptions.PosNormalTexTan);

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

            ImmediateContext.InputAssembler.InputLayout = _layout;
            ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            _camera.UpdateViewMatrix();

            var view = _camera.View;
            var proj = _camera.Proj;

            // directional lighting
            ImmediateContext.OutputMerger.DepthStencilState = RenderStates.LessEqualDSS;
            ForwardSetup();

            var activeTech = _effect.Directional;
            for (var p = 0; p < activeTech.Description.PassCount; p++) {
                var pass = activeTech.GetPassByIndex(p);
                _bunnyInstance.Draw(ImmediateContext, pass, view, proj, DrawDirectional);
            }
            
            // point lights
            var prevBlend = ImmediateContext.OutputMerger.BlendState;
            ImmediateContext.OutputMerger.BlendState = _additiveBlend;
            for (int i = 0; i < NumLights; i++) {
                PointSetup(i);
                activeTech = _effect.PointLight;
                for (var p = 0; p < activeTech.Description.PassCount; p++) {
                    var pass = activeTech.GetPassByIndex(p);
                    _bunnyInstance.Draw(ImmediateContext, pass, view, proj, DrawPoint);
                }
            }


            ImmediateContext.OutputMerger.BlendState = prevBlend;
            SwapChain.Present(0, PresentFlags.None);
        }

        private void PointSetup(int i) {
            _effect.SetLightPosition(_lightPositions[i]);
            _effect.SetLightRangeRcp(1.0f/_lightRange);
            _effect.SetLightColor(_lightColor[i]);
        }

        private static Vector3 GammaToLinear(Vector3 c) {
            return new Vector3(c.X * c.X, c.Y * c.Y, c.Z * c.Z);
        }
        private void ForwardSetup() {
            _effect.SetAmbientDown(GammaToLinear(_ambientLowerColor));//lowerGamma);
            _effect.SetAmbientRange(GammaToLinear(_ambientUpperColor) - GammaToLinear(_ambientLowerColor)); //range);
            _effect.SetDirectLightColor(GammaToLinear(_dirLightColor));
            _effect.SetDirectLightDirection(-_dirLightDirection);
        }

        private void DrawDirectional(DeviceContext dc, EffectPass pass, Matrix view, Matrix proj) {
            var model = _bunnyInstance.Model;
            var world = _bunnyInstance.World;
            var wit = MathF.InverseTranspose(world);

            _effect.SetWorld(world);
            _effect.SetWorldViewProj(world * view * proj);
            _effect.SetWorldInvTranspose(wit);
            _effect.SetEyePosition(_camera.Position);
            _effect.SetSpecularExponent(250.0f);
            _effect.SetSpecularIntensity(0.25f);

            for (var i = 0; i < model.SubsetCount; i++) {
                _effect.SetDiffuseMap(model.DiffuseMapSRV[i]);
                pass.Apply(ImmediateContext);
                model.ModelMesh.Draw(ImmediateContext, i);
            }
        }
        private void DrawPoint(DeviceContext dc, EffectPass pass, Matrix view, Matrix proj) {
            var model = _bunnyInstance.Model;
            var world = _bunnyInstance.World;
            var wit = MathF.InverseTranspose(world);

            _effect.SetWorld(world);
            _effect.SetWorldViewProj(world * view * proj);
            _effect.SetWorldInvTranspose(wit);
            _effect.SetEyePosition(_camera.Position);
            _effect.SetSpecularExponent(250.0f);
            _effect.SetSpecularIntensity(0.25f);

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


        static void Main(string[] args) {
            Configuration.EnableObjectTracking = true;
            var app = new PointLightDemo(Process.GetCurrentProcess().Handle);
            if (!app.Init()) {
                return;
            }
            app.Run();
        }

    }
}
