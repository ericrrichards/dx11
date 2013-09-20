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
using Core.Model;
using Core.Model.dx9;
using SlimDX;
using SlimDX.DXGI;
using SlimDX.Direct3D11;

namespace AssimpModel {
    using SkinnedModelInstance = Core.Model.SkinnedModelInstance;

    class AssimpModelDemo : D3DApp {

        private TextureManager _texMgr;

        private BasicModel _treeModel;
        private BasicModel _stoneModel;
        private BasicModelInstance _modelInstance;
        private BasicModelInstance _stoneInstance;
        private SkinnedModel _drone;
        private SkinnedModelInstance _droneInstance;

        private readonly DirectionalLight[] _dirLights;

        private FpsCamera _camera;
        private Point _lastMousePos;
        private bool _disposed;
        private SkinnedModel _soldier;
        private SkinnedModelInstance _soldierInstance;

        protected AssimpModelDemo(IntPtr hInstance) : base(hInstance) {
            MainWindowCaption = "Assimp Model Demo";
            _lastMousePos = new Point();

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

            _modelInstance = new BasicModelInstance {
                Model = _treeModel,
                World = Matrix.RotationX(MathF.PI/2)
            };

            _stoneModel = new BasicModel(Device, _texMgr, "Models/stone.x", "Textures");
            _stoneInstance = new BasicModelInstance {
                Model = _stoneModel,
                World = Matrix.Scaling(0.1f, 0.1f, 0.1f)*Matrix.Translation(2, 0, 2)
            };
            _drone = new SkinnedModel(Device, _texMgr, "Models/drone.x", "Textures", true);
            _droneInstance = new SkinnedModelInstance(){
                ClipName = "Run",
                World =Matrix.Identity,
                TimePos = 0.0f,
                Model = _drone

            };

            _soldier = new SkinnedModel(Device, _texMgr, "Models/soldier.x", "Textures", true);
            _soldierInstance = new SkinnedModelInstance() {
                ClipName = "Run",
                Model = _soldier,
                World = Matrix.Translation(10, 0, 0),
                TimePos = 0.0f
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
            _soldierInstance.Update(dt);
        }
        public override void DrawScene() {
            base.DrawScene();
            ImmediateContext.ClearRenderTargetView(RenderTargetView, Color.Silver);
            ImmediateContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth|DepthStencilClearFlags.Stencil, 1.0f, 0 );

            ImmediateContext.InputAssembler.InputLayout = InputLayouts.PosNormalTexTan;
            ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            _camera.UpdateViewMatrix();
            var viewProj = _camera.ViewProj;

            Effects.NormalMapFX.SetDirLights(_dirLights);
            Effects.NormalMapFX.SetEyePosW(_camera.Position);
            Effects.BasicFX.SetDirLights(_dirLights);
            Effects.BasicFX.SetEyePosW(_camera.Position);

            var activeTech = Effects.NormalMapFX.Light3TexTech;
            for (int p = 0; p < activeTech.Description.PassCount; p++) {
                var world = _modelInstance.World;
                var wit = MathF.InverseTranspose(world);
                var wvp = world*viewProj;

                Effects.NormalMapFX.SetWorld(world);
                Effects.NormalMapFX.SetWorldInvTranspose(wit);
                Effects.NormalMapFX.SetWorldViewProj(wvp);
                Effects.NormalMapFX.SetTexTransform(Matrix.Identity);

                for (int i = 0; i < _modelInstance.Model.SubsetCount; i++) {
                    Effects.NormalMapFX.SetMaterial(_modelInstance.Model.Materials[i]);
                    Effects.NormalMapFX.SetDiffuseMap(_modelInstance.Model.DiffuseMapSRV[i]);
                    Effects.NormalMapFX.SetNormalMap(_modelInstance.Model.NormalMapSRV[i]);

                    activeTech.GetPassByIndex(p).Apply(ImmediateContext);
                    //_modelInstance.Model.ModelMesh.Draw(ImmediateContext, i);
                }
                world = _stoneInstance.World;
                wit = MathF.InverseTranspose(world);
                wvp = world * viewProj;

                Effects.NormalMapFX.SetWorld(world);
                Effects.NormalMapFX.SetWorldInvTranspose(wit);
                Effects.NormalMapFX.SetWorldViewProj(wvp);
                Effects.NormalMapFX.SetTexTransform(Matrix.Identity);

                for (int i = 0; i < _modelInstance.Model.SubsetCount; i++) {
                    Effects.NormalMapFX.SetMaterial(_stoneInstance.Model.Materials[i]);
                    Effects.NormalMapFX.SetDiffuseMap(_stoneInstance.Model.DiffuseMapSRV[i]);
                    Effects.NormalMapFX.SetNormalMap(_stoneInstance.Model.NormalMapSRV[i]);

                    activeTech.GetPassByIndex(p).Apply(ImmediateContext);
                    //_stoneInstance.Model.ModelMesh.Draw(ImmediateContext, i);
                }
                
            }
            ImmediateContext.InputAssembler.InputLayout = InputLayouts.PosNormalTexTanSkinned;
            for (int p = 0; p < Effects.BasicFX.Light3TexSkinnedTech.Description.PassCount; p++) {
                var world = _droneInstance.World;
                var wit = MathF.InverseTranspose(world);
                var wvp = world * viewProj;

                Effects.BasicFX.SetWorld(world);
                Effects.BasicFX.SetWorldInvTranspose(wit);
                Effects.BasicFX.SetWorldViewProj(wvp);
                Effects.BasicFX.SetTexTransform(Matrix.Identity);
                
                Effects.BasicFX.SetBoneTransforms(_droneInstance.FinalTransforms);
                
                //ImmediateContext.Rasterizer.State = RenderStates.NoCullRS;
                for (int i = 0; i < _droneInstance.Model.SubsetCount; i++) {
                    Effects.BasicFX.SetMaterial(_droneInstance.Model.Materials[i]);
                    Effects.BasicFX.SetDiffuseMap(_droneInstance.Model.DiffuseMapSRV[i]);
                    //Effects.NormalMapFX.SetNormalMap(null);

                    Effects.BasicFX.Light3TexSkinnedTech.GetPassByIndex(p).Apply(ImmediateContext);
                    _droneInstance.Model.ModelMesh.Draw(ImmediateContext, i);
                }
                world = _soldierInstance.World;
                wit = MathF.InverseTranspose(world);
                wvp = world * viewProj;

                Effects.BasicFX.SetWorld(world);
                Effects.BasicFX.SetWorldInvTranspose(wit);
                Effects.BasicFX.SetWorldViewProj(wvp);
                Effects.BasicFX.SetTexTransform(Matrix.Identity);

                Effects.BasicFX.SetBoneTransforms(_soldierInstance.FinalTransforms);

                //ImmediateContext.Rasterizer.State = RenderStates.NoCullRS;
                for (int i = 0; i < _soldierInstance.Model.SubsetCount; i++) {
                    Effects.BasicFX.SetMaterial(_soldierInstance.Model.Materials[i]);
                    Effects.BasicFX.SetDiffuseMap(_soldierInstance.Model.DiffuseMapSRV[i]);
                    //Effects.NormalMapFX.SetNormalMap(null);

                    Effects.BasicFX.Light3TexSkinnedTech.GetPassByIndex(p).Apply(ImmediateContext);
                    _soldierInstance.Model.ModelMesh.Draw(ImmediateContext, i);
                }
            }
            //_droneInstance.DrawSkeleton(ImmediateContext, _camera);
            
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
            var app = new AssimpModelDemo(Process.GetCurrentProcess().Handle);
            if (!app.Init()) {
                return;
            }
            app.Run();
        }
    }
}
