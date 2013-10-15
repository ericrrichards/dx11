using System;

namespace SkinnedModels {
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

    class SkinnedModelDemo : D3DApp {

        private TextureManager _texMgr;

        private readonly DirectionalLight[] _dirLights;

        private readonly FpsCamera _camera;
        private Point _lastMousePos;
        private bool _disposed;

        private SkinnedModel _drone;
        private SkinnedModelInstance _droneInstance;
        private SkinnedModel _soldier;
        private SkinnedModelInstance _soldierInstance;
        private SkinnedModelInstance _mageInstance;
        private SkinnedModel _mage;
        private BasicModel _grid;
        private BasicModelInstance _gridInstance;

        private SkinnedModelDemo(IntPtr hInstance)
            : base(hInstance) {
            MainWindowCaption = "Skinned Model Demo";
            _lastMousePos = new Point();
            //Enable4xMsaa = true;

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
                    Util.ReleaseCom(ref _drone);
                    Util.ReleaseCom(ref _soldier);
                    Util.ReleaseCom(ref _mage);
                    Util.ReleaseCom(ref _grid);
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

            _drone = new SkinnedModel(Device, _texMgr, "Models/drone.x", "Textures", true);
            _droneInstance = new SkinnedModelInstance(
                "Attack",
                Matrix.RotationY(MathF.PI),
                _drone

            );
            foreach (var clip in _droneInstance.Clips) {
                _droneInstance.AddClip(clip);
            }
            _droneInstance.LoopClips = true;
            

            _mage = new SkinnedModel(Device, _texMgr, "Models/magician.x", "textures", true);

            _mageInstance = new SkinnedModelInstance(
                "Attack",
                Matrix.RotationY(MathF.PI)*Matrix.Translation(4.0f, 0, 0),
                _mage
            );
            foreach (var clip in _mageInstance.Clips) {
                _mageInstance.AddClip(clip);
            }
            _mageInstance.LoopClips = true;

            _soldier = new SkinnedModel(Device, _texMgr, "Models/soldier.x", "Textures", true);
            _soldierInstance = new SkinnedModelInstance (
                "Attack",
                Matrix.RotationY(MathF.PI)*Matrix.Translation(10, 0, 0),
                _soldier
            );

            foreach (var clip in _soldierInstance.Clips) {
                _soldierInstance.AddClip(clip);
            }
            _soldierInstance.LoopClips = true;

            _grid = BasicModel.CreateGrid(Device, 30, 30, 60, 60);
            _grid.DiffuseMapSRV[0] = (_texMgr.CreateTexture("Textures/floor.dds"));
            _grid.NormalMapSRV[0]= (_texMgr.CreateTexture("Textures/floor_nmap.dds"));
            _gridInstance = new BasicModelInstance() {
                Model = _grid,
                World = Matrix.Translation(0, -1.5f, 0)
                
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
            _droneInstance.Update(dt);
            _mageInstance.Update(dt);
            _soldierInstance.Update(dt);


        }

        public override void DrawScene() {
            base.DrawScene();
            ImmediateContext.ClearRenderTargetView(RenderTargetView, Color.Silver);
            ImmediateContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);

            ImmediateContext.InputAssembler.InputLayout = InputLayouts.PosNormalTexTanSkinned;
            ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            _camera.UpdateViewMatrix();
            var viewProj = _camera.ViewProj;

            Effects.NormalMapFX.SetDirLights(_dirLights);
            Effects.NormalMapFX.SetEyePosW(_camera.Position);



            for (int p = 0; p < Effects.NormalMapFX.Light3TexSkinnedTech.Description.PassCount; p++) {
                var world = _mageInstance.World;
                var wit = MathF.InverseTranspose(world);
                var wvp = world * viewProj;

                Effects.NormalMapFX.SetWorld(world);
                Effects.NormalMapFX.SetWorldInvTranspose(wit);
                Effects.NormalMapFX.SetWorldViewProj(wvp);
                Effects.NormalMapFX.SetTexTransform(Matrix.Identity);

                Effects.NormalMapFX.SetBoneTransforms(_mageInstance.FinalTransforms);


                for (int i = 0; i < _mageInstance.Model.SubsetCount; i++) {
                    Effects.NormalMapFX.SetMaterial(_mageInstance.Model.Materials[i]);
                    Effects.NormalMapFX.SetDiffuseMap(_mageInstance.Model.DiffuseMapSRV[i]);
                    Effects.NormalMapFX.SetNormalMap(_mageInstance.Model.NormalMapSRV[i]);


                    Effects.NormalMapFX.Light3TexSkinnedTech.GetPassByIndex(p).Apply(ImmediateContext);
                    _mageInstance.Model.ModelMesh.Draw(ImmediateContext, i);
                }
                world = _droneInstance.World;
                wit = MathF.InverseTranspose(world);
                wvp = world * viewProj;

                Effects.NormalMapFX.SetWorld(world);
                Effects.NormalMapFX.SetWorldInvTranspose(wit);
                Effects.NormalMapFX.SetWorldViewProj(wvp);
                Effects.NormalMapFX.SetTexTransform(Matrix.Identity);

                Effects.NormalMapFX.SetBoneTransforms(_droneInstance.FinalTransforms);


                for (int i = 0; i < _droneInstance.Model.SubsetCount; i++) {
                    Effects.NormalMapFX.SetMaterial(_droneInstance.Model.Materials[i]);
                    Effects.NormalMapFX.SetDiffuseMap(_droneInstance.Model.DiffuseMapSRV[i]);
                    Effects.NormalMapFX.SetNormalMap(_droneInstance.Model.NormalMapSRV[i]);

                    Effects.NormalMapFX.Light3TexSkinnedTech.GetPassByIndex(p).Apply(ImmediateContext);
                    _droneInstance.Model.ModelMesh.Draw(ImmediateContext, i);
                }
                world = _soldierInstance.World;
                wit = MathF.InverseTranspose(world);
                wvp = world * viewProj;

                Effects.NormalMapFX.SetWorld(world);
                Effects.NormalMapFX.SetWorldInvTranspose(wit);
                Effects.NormalMapFX.SetWorldViewProj(wvp);
                Effects.NormalMapFX.SetTexTransform(Matrix.Identity);

                Effects.NormalMapFX.SetBoneTransforms(_soldierInstance.FinalTransforms);


                for (int i = 0; i < _soldierInstance.Model.SubsetCount; i++) {
                    Effects.NormalMapFX.SetMaterial(_soldierInstance.Model.Materials[i]);
                    Effects.NormalMapFX.SetDiffuseMap(_soldierInstance.Model.DiffuseMapSRV[i]);
                    Effects.NormalMapFX.SetNormalMap(_soldierInstance.Model.NormalMapSRV[i]);

                    Effects.NormalMapFX.Light3TexSkinnedTech.GetPassByIndex(p).Apply(ImmediateContext);
                    _soldierInstance.Model.ModelMesh.Draw(ImmediateContext, i);
                }
                
            }
            ImmediateContext.InputAssembler.InputLayout = InputLayouts.PosNormalTexTan;
            for (int p = 0; p < Effects.NormalMapFX.Light3Tech.Description.PassCount; p++) {
                _gridInstance.Draw(ImmediateContext, Effects.NormalMapFX.Light3Tech.GetPassByIndex(p), viewProj);
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
            var app = new SkinnedModelDemo(Process.GetCurrentProcess().Handle);
            if (!app.Init()) {
                return;
            }
            app.Run();
        }
    }
}
