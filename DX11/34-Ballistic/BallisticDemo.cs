using System;
using System.Collections.Generic;

namespace _34_Ballistic {
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

    public enum ShotType {
        Pistol,
        Artillery,
        Fireball,
        Laser
    };

    internal struct AmmoRound {
        public Particle Particle { get; set; }
        public ShotType ShotType { get; set; }
        public float StartTime { get; set; }
        public BasicModelInstance Model { get; set; }

        public void Render(DeviceContext dc, EffectPass pass, Matrix view, Matrix proj) {
            var position = Particle.Position;
            Model.World = Matrix.Scaling(new Vector3(1 + Particle.Mass / 100)) * Matrix.Translation(position);

            Model.Draw(dc, pass, view, proj);

        }

    };


    class BallisticDemo : D3DApp {

        private const int MaxRounds = 16;
        readonly List<AmmoRound> _ammo = new List<AmmoRound>();
        private ShotType _currentShotType;

        private readonly DirectionalLight[] _dirLights;

        private readonly FpsCamera _camera;
        private BasicModel _gridModel;
        private BasicModel _sphereModel;
        


        private BasicModelInstance _grid;
        private BasicModelInstance _sphere;
        private BasicModel _cylinderModel;
        private BasicModelInstance _cylinder;

        private Point _lastMousePos;
        private bool _disposed;
        private TextureManager _texMgr;
        private float _fireDelay = 0.5f;

        private BallisticDemo(IntPtr hInstance)
            : base(hInstance) {
            _currentShotType = ShotType.Laser;

            MainWindowCaption = "Ballistic Demo";

            _lastMousePos = new Point();

            _camera = new FpsCamera();
            _camera.LookAt(new Vector3(10, 2, -10), new Vector3(0, 1, 0), Vector3.UnitY);

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
                    Util.ReleaseCom(ref _gridModel);
                    Util.ReleaseCom(ref _sphereModel);
                    Util.ReleaseCom(ref _cylinderModel);
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
            if (!base.Init())
                return false;
            Effects.InitAll(Device);
            InputLayouts.InitAll(Device);
            RenderStates.InitAll(Device);

            _texMgr = new TextureManager();
            _texMgr.Init(Device);

            _gridModel = new BasicModel();
            _gridModel.CreateGrid(Device, 20, 20, 40, 40);
            _gridModel.Materials[0] = new Material { Diffuse = Color.SaddleBrown, Specular = new Color4(16, .9f, .9f, .9f) };
            _gridModel.DiffuseMapSRV[0] = _texMgr.CreateTexture("Textures/floor.dds");
            _gridModel.NormalMapSRV[0] = _texMgr.CreateTexture("textures/floor_nmap.dds");

            _sphereModel = new BasicModel();
            _sphereModel.CreateSphere(Device, 0.3f, 5, 4);
            _sphereModel.Materials[0] = new Material { Ambient = Color.Blue, Diffuse = Color.Blue, Specular = new Color4(64.0f, 1.0f, 1.0f, 1.0f) };
            _sphereModel.NormalMapSRV[0] = _texMgr.CreateTexture("Textures/stones_nmap.dds");

            _cylinderModel = new BasicModel();
            _cylinderModel.CreateCylinder(Device, 1, 1, 3, 20, 20);
            _cylinderModel.Materials[0] = new Material { Ambient = Color.Green, Diffuse = Color.Green, Specular = new Color4(64.0f, 1.0f, 1.0f, 1.0f) };
            _cylinderModel.NormalMapSRV[0] = _texMgr.CreateTexture("Textures/stones_nmap.dds");

            _grid = new BasicModelInstance(_gridModel) {
                TexTransform = Matrix.Scaling(10, 10, 1),
                World = Matrix.Scaling(10, 1, 10) * Matrix.Translation(0, 0, 90)
            };

            _sphere = new BasicModelInstance(_sphereModel);

            _cylinder = new BasicModelInstance(_cylinderModel) {
                World = Matrix.Translation(0, 1.5f, 0)
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

            for (var index = 0; index < _ammo.Count; index++) {
                var shot = _ammo[index];
                shot.Particle.Integrate(dt);
                if (shot.Particle.Position.Y < 0.0f || shot.StartTime + 10.0f < Timer.TotalTime || shot.Particle.Position.Z > 200.0f) {
                    _ammo.Remove(shot);
                }
            }
            _fireDelay -= dt;
            if (Util.IsKeyDown(Keys.D1)) {
                _currentShotType = ShotType.Pistol;
            } else if (Util.IsKeyDown(Keys.D2)) {
                _currentShotType = ShotType.Artillery;
            } else if (Util.IsKeyDown(Keys.D3)) {
                _currentShotType = ShotType.Fireball;
            } else if (Util.IsKeyDown(Keys.D4)) {
                _currentShotType = ShotType.Laser;
            } else if (Util.IsKeyDown(Keys.Space) && _fireDelay < 0) {
                Fire();
                _fireDelay = 0.2f;
            }
        }
        protected override void OnMouseDown(object sender, MouseEventArgs mouseEventArgs) {
            _lastMousePos = mouseEventArgs.Location;
            Window.Capture = true;

        }

        private void Fire() {
            if (_ammo.Count >= MaxRounds)
                return;
            var shot = new AmmoRound { Model = _sphere };
            var firingPoint = new Vector3(0, 1.5f, 0.0f);
            switch (_currentShotType) {
                case ShotType.Pistol:
                    shot.Particle = new Particle(firingPoint,
                        new Vector3(0, 0, 35), // 35 m/s
                        new Vector3(0, -1, 0), // small amount of gravity
                        2.0f // 2 kg
                        ) { Damping = 0.99f };
                    break;
                case ShotType.Artillery:
                    shot.Particle = new Particle(
                        firingPoint,
                        new Vector3(0, 30, 40), // 50 m/s
                        new Vector3(0, -20, 0), // large amount of gravity
                        200.0f // 200 kg
                        ) { Damping = 0.99f };
                    break;
                case ShotType.Fireball:
                    shot.Particle = new Particle(
                        firingPoint,
                        new Vector3(0, 0, 10), // 10 m/s
                        new Vector3(0, 0.6f, 0), // float up slightly
                        1.0f // 1 kg
                        ) { Damping = 0.9f };
                    break;
                case ShotType.Laser:
                    shot.Particle = new Particle(
                        firingPoint,
                        new Vector3(0, 0, 100), // 100 m/s
                        new Vector3(0, 0, 0),  // no gravity
                        0.1f // 100 grams
                        ) { Damping = 0.99f };
                    break;
            }

            shot.Particle.Position = firingPoint;
            shot.StartTime = Timer.TotalTime;
            shot.ShotType = _currentShotType;

            shot.Particle.ClearAccumulator();

            _ammo.Add(shot);
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

            Effects.NormalMapFX.SetDirLights(_dirLights);
            Effects.NormalMapFX.SetEyePosW(_camera.Position);

            var floorTech = Effects.NormalMapFX.Light3TexTech;
            var activeTech = Effects.NormalMapFX.Light3Tech;

            ImmediateContext.InputAssembler.InputLayout = InputLayouts.PosNormalTexTan;
            ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            if (Util.IsKeyDown(Keys.W)) {
                ImmediateContext.Rasterizer.State = RenderStates.WireframeRS;
            }

            for (var p = 0; p < floorTech.Description.PassCount; p++) {
                var pass = activeTech.GetPassByIndex(p);
                _grid.Draw(ImmediateContext, pass, view, proj);
            }
            for (var p = 0; p < activeTech.Description.PassCount; p++) {
                var pass = activeTech.GetPassByIndex(p);
                foreach (var ammoRound in _ammo) {
                    ammoRound.Render(ImmediateContext, pass, view, proj);
                }
                _cylinder.Draw(ImmediateContext, pass, view, proj);
            }


            SwapChain.Present(0, PresentFlags.None);
            ImmediateContext.Rasterizer.State = null;
        }

        static void Main(string[] args) {
            Configuration.EnableObjectTracking = true;
            var app = new BallisticDemo(Process.GetCurrentProcess().Handle);
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
