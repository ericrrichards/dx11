using System;
using System.Collections.Generic;

namespace _35_Fireworks {
    using System.Diagnostics;
    using System.Drawing;
    using System.Windows.Forms;

    using Core;
    using Core.Camera;
    using Core.FX;
    using Core.Model;
    using Core.Physics;

    using SlimDX;
    using SlimDX.Direct3D11;
    using SlimDX.DXGI;

    public class Firework : Particle {
        public int Type { get; private set; }
        private float Age { get; set; }
        private BasicModelInstance Model { get; set; }

        public Firework(int type, float age, BasicModel m) {
            Type = type;
            Age = age;
            Model = new BasicModelInstance(m);
        }

        public bool Update(float dt) {
            Integrate(dt);
            Age -= dt;
            return Age < 0 || Position.Y < 0;
        }

        public void Render(DeviceContext dc, EffectPass pass, Matrix view, Matrix proj) {
            Model.World = Matrix.Translation(Position);

            Model.Draw(dc, pass, view, proj, RenderMode.Basic);
        }
    }

    public class FireWorkRule :DisposableClass {
        public class Payload {
            public int Type { get; private set; }
            public int Count { get; private set; }

            public Payload(int type, int count) {
                Type = type;
                Count = count;
            }
        }

        private int Type { get; set; }
        private float MinAge { get; set; }
        private float MaxAge { get; set; }
        private Vector3 MinVelocity { get; set; }
        private Vector3 MaxVelocity { get; set; }
        private float Damping { get; set; }

        public List<Payload> Payloads { get; private set; }


        private BasicModel _model;
        private bool _disposed;

        private FireWorkRule() {
            Payloads = new List<Payload>();
        }

        public FireWorkRule(int type, float minAge, float maxAge, Vector3 minVel, Vector3 maxVel, float damping, BasicModel model)
            : this() {
            Type = type;
            MinAge = minAge;
            MaxAge = maxAge;
            MinVelocity = minVel;
            MaxVelocity = maxVel;
            Damping = damping;
            _model = model;
        }
        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Util.ReleaseCom(ref _model);
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        public Firework CreateFirework(Firework parent = null) {
            var f = new Firework(Type, MathF.Rand(MinAge, MaxAge), _model);
            var vel = new Vector3();
            if (parent != null) {
                f.Position = parent.Position;
                vel += parent.Velocity;
            } else {
                var x = MathF.Rand(-1, 2)*5;
                f.Position = new Vector3(x, 0, 0);
            }
            vel += MathF.RandVector(MinVelocity, MaxVelocity);
            f.Velocity = vel;
            f.Mass = 1;
            f.Damping = Damping;
            f.Acceleration = Constants.Gravity;
            f.ClearAccumulator();
            return f;
        }

    }



    class FireworksDemo : D3DApp {
        private readonly List<Firework> _fireworks = new List<Firework>();
        private readonly List<FireWorkRule> _rules = new List<FireWorkRule>();

        private readonly DirectionalLight[] _dirLights;

        private readonly LookAtCamera _camera;
        private BasicModel _gridModel;
        private BasicModelInstance _grid;

        private Point _lastMousePos;
        private bool _disposed;
        private TextureManager _texMgr;
        private float _fireDelay = 0.5f;
        private Sky _sky;

        private FireworksDemo(IntPtr hInstance)
            : base(hInstance) {
            MainWindowCaption = "Fireworks Demo";

            _lastMousePos = new Point();

            _camera = new LookAtCamera();
            _camera.LookAt(new Vector3(10, 2, -10), new Vector3(0, 1, 0), Vector3.UnitY);

            _dirLights = new[] {
                new DirectionalLight {
                    Ambient = new Color4( 0.9f, 0.9f, 0.9f),
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
                    Util.ReleaseCom(ref _gridModel);
                    Util.ReleaseCom(ref _texMgr);

                    foreach (var t in _rules) {
                        var fireWorkRule = t;
                        Util.ReleaseCom(ref fireWorkRule);
                    }
                    _rules.Clear();

                    Effects.DestroyAll();
                    InputLayouts.DestroyAll();
                    RenderStates.DestroyAll();
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        public override bool Init() {
            if (!base.Init())
                return false;
            Effects.InitAll(Device);
            InputLayouts.InitAll(Device);
            RenderStates.InitAll(Device);

            _sky = new Sky(Device, "Textures/grasscube1024.dds", 5000.0f);

            _texMgr = new TextureManager();
            _texMgr.Init(Device);

            _gridModel = new BasicModel();
            _gridModel.CreateGrid(Device, 20, 20, 40, 40);
            _gridModel.Materials[0] = new Material { Diffuse = Color.SaddleBrown, Specular = new Color4(16, .9f, .9f, .9f) };
            _gridModel.DiffuseMapSRV[0] = _texMgr.CreateTexture("Textures/floor.dds");
            _gridModel.NormalMapSRV[0] = _texMgr.CreateTexture("textures/floor_nmap.dds");

            _grid = new BasicModelInstance(_gridModel) {
                TexTransform = Matrix.Scaling(10, 10, 1),
                World = Matrix.Scaling(10, 1, 10) * Matrix.Translation(0, 0, 90)
            };
            InitFireworksRules();

            return true;
        }

        private void InitFireworksRules() {
            
            var m1 = CreateFireworkModel(Color.Red);
            var rule = new FireWorkRule(1, 0.5f, 1.4f, new Vector3(-5, 25, -5), new Vector3(5, 28, 5), 0.1f, m1);
            rule.Payloads.Add(new FireWorkRule.Payload(3,5));
            rule.Payloads.Add(new FireWorkRule.Payload(5,5));

            _rules.Add(rule);

            m1 = CreateFireworkModel(Color.Orange);
            rule = new FireWorkRule(2, 0.5f, 1.0f, new Vector3(-5, 10, -5), new Vector3(5, 20, 5), 0.8f, m1);
            rule.Payloads.Add(new FireWorkRule.Payload(4, 2));

            _rules.Add(rule);
            m1 = CreateFireworkModel(Color.Yellow);
            rule = new FireWorkRule(3, 0.5f, 1.5f, new Vector3(-5,-5, -5), new Vector3(5, 5, 5), 0.1f, m1);

            _rules.Add(rule);

            m1 = CreateFireworkModel(Color.Green);
            rule = new FireWorkRule(4, 0.25f, 0.5f, new Vector3(-20, 5, -5), new Vector3(20, 5, 5), 0.2f, m1);

            _rules.Add(rule);

            m1 = CreateFireworkModel(Color.Cyan);
            rule = new FireWorkRule(5, 0.5f, 1.0f, new Vector3(-20, 2, -5), new Vector3(20, 18, 5), 0.01f, m1);
            rule.Payloads.Add(new FireWorkRule.Payload(3, 5));

            _rules.Add(rule);

            m1 = CreateFireworkModel(Color.BlueViolet);
            rule = new FireWorkRule(6, 3f, 5f, new Vector3(-5, 5, -5), new Vector3(5, 10, 5), 0.95f, m1);
            

            _rules.Add(rule);

            m1 = CreateFireworkModel(Color.Magenta);
            rule = new FireWorkRule(7, 4f, 5f, new Vector3(-5, 50, -5), new Vector3(5, 60, 5), 0.01f, m1);
            rule.Payloads.Add(new FireWorkRule.Payload(8, 10));

            _rules.Add(rule);

            m1 = CreateFireworkModel(Color.White);
            rule = new FireWorkRule(8, 0.25f, 0.5f, new Vector3(-1, -1, -1), new Vector3(1, 1, 1), 0.01f, m1);
            
            _rules.Add(rule);

            m1 = CreateFireworkModel(Color.Pink);
            rule = new FireWorkRule(9, 3f, 5f, new Vector3(-15, 10, -5), new Vector3(15, 15, 5), 0.95f, m1);

            _rules.Add(rule);

        }

        private BasicModel CreateFireworkModel(Color diffuse) {
            var m1 = new BasicModel();
            m1.CreateSphere(Device, .2f, 5, 5);
            m1.Materials[0] = new Material { Ambient = Color.White, Diffuse = diffuse, Specular = new Color4(128.0f, 1.0f, 1.0f, 1.0f) };
            
            return m1;
        }

        private void Create(int type, Firework parent) {
            var rule = _rules[type - 1];
            _fireworks.Add(rule.CreateFirework(parent));
        }

        private void Create(int type, int number, Firework parent) {
            for (var i = 0; i < number; i++) {
                Create(type, parent);
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
                _camera.Zoom(-dt*10);
            }
            if (Util.IsKeyDown(Keys.PageDown)) {
                _camera.Zoom(+dt*10);
            }

            _camera.UpdateViewMatrix();


            for (var i = 0; i < _fireworks.Count; i++) {
                var firework = _fireworks[i];
                if (firework.Type > 0) {
                    if (firework.Update(dt)) {
                        var rule = _rules[firework.Type - 1];

                        foreach (var payload in rule.Payloads  ) {
                            Create(payload.Type, payload.Count, firework);
                        }
                        _fireworks.Remove(firework);
                    }
                }
            }
            _fireDelay -= dt;
            if (Util.IsKeyDown(Keys.D1) && _fireDelay < 0) {
                Create(1, null);
                _fireDelay = 0.2f;
            }
            if (Util.IsKeyDown(Keys.D2) && _fireDelay < 0) {
                Create(2, null);
                _fireDelay = 0.2f;
            }
            if (Util.IsKeyDown(Keys.D3) && _fireDelay < 0) {
                Create(3, null);
                _fireDelay = 0.2f;
            }
            if (Util.IsKeyDown(Keys.D4) && _fireDelay < 0) {
                Create(4, null);
                _fireDelay = 0.2f;
            }
            if (Util.IsKeyDown(Keys.D5) && _fireDelay < 0) {
                Create(5, null);
                _fireDelay = 0.2f;
            }
            if (Util.IsKeyDown(Keys.D6) && _fireDelay < 0) {
                Create(6, null);
                _fireDelay = 0.2f;
            }
            if (Util.IsKeyDown(Keys.D7) && _fireDelay < 0) {
                Create(7, null);
                _fireDelay = 0.2f;
            }
            if (Util.IsKeyDown(Keys.D8) && _fireDelay < 0) {
                Create(8, null);
                _fireDelay = 0.2f;
            }
            if (Util.IsKeyDown(Keys.D9) && _fireDelay < 0) {
                Create(9, null);
                _fireDelay = 0.2f;
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
        protected override void OnMouseWheel(object sender, MouseEventArgs e) {
            var zoom = e.Delta;
            if (zoom > 0) {
                _camera.Zoom(-1);
            } else {
                _camera.Zoom(1);
            }



        }

        public override void DrawScene() {
            ImmediateContext.ClearRenderTargetView(RenderTargetView, Color.Silver);
            ImmediateContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);

            var view = _camera.View;
            var proj = _camera.Proj;

            Effects.NormalMapFX.SetDirLights(_dirLights);
            Effects.NormalMapFX.SetEyePosW(_camera.Position);
            Effects.BasicFX.SetDirLights(_dirLights);
            Effects.BasicFX.SetEyePosW(_camera.Position);

            var floorTech = Effects.NormalMapFX.Light3TexTech;
            var activeTech = Effects.BasicFX.Light3Tech;

            ImmediateContext.InputAssembler.InputLayout = InputLayouts.PosNormalTexTan;
            ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            if (Util.IsKeyDown(Keys.W)) {
                ImmediateContext.Rasterizer.State = RenderStates.WireframeRS;
            }

            for (var p = 0; p < floorTech.Description.PassCount; p++) {
                var pass = floorTech.GetPassByIndex(p);
                _grid.Draw(ImmediateContext, pass, view, proj);
            }
            for (var p = 0; p < activeTech.Description.PassCount; p++) {
                var pass = activeTech.GetPassByIndex(p);
                foreach (var firework in _fireworks) {
                    firework.Render(ImmediateContext, pass, view, proj);
                }
            }

            _sky.Draw(ImmediateContext, _camera);


            ImmediateContext.Rasterizer.State = null;
            ImmediateContext.OutputMerger.DepthStencilState = null;
            ImmediateContext.OutputMerger.DepthStencilReference = 0;


            SwapChain.Present(0, PresentFlags.None);
            ImmediateContext.Rasterizer.State = null;
        }


        static void Main() {
            Configuration.EnableObjectTracking = true;
            var app = new FireworksDemo(Process.GetCurrentProcess().Handle);
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
