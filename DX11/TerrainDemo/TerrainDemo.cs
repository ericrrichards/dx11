using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Core;
using Core.Camera;
using Core.FX;
using Core.Terrain;
using SlimDX;
using SlimDX.DXGI;
using SlimDX.Direct3D11;

namespace TerrainDemo {
    using System.Runtime.InteropServices;

    using Core.Model;

    using Buffer = SlimDX.Direct3D11.Buffer;
    using MapFlags = SlimDX.Direct3D11.MapFlags;

    class TerrainDemo :D3DApp {
        private Sky _sky;
        private Terrain _terrain;
        private readonly DirectionalLight[] _dirLights;

        private readonly LookAtCamera _camera;

        private bool _camWalkMode;
        private Point _lastMousePos;
        private bool _disposed;

        private BasicModel _treeModel;
        private TextureManager _txMgr;
        private List<BasicModelInstance> _treeInstances;
        private const int NumTrees = 10000;
        private List<Matrix> _instancedTrees;
        private int _visibleTrees;
        private Buffer _instanceBuffer;

        private TerrainDemo(IntPtr hInstance) : base(hInstance) {
            MainWindowCaption = "Terrain Demo";
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
                    Util.ReleaseCom(ref _treeModel);
                    Util.ReleaseCom(ref _instanceBuffer);

                    Effects.DestroyAll();
                    InputLayouts.DestroyAll();
                    RenderStates.DestroyAll();
                    Util.ReleaseCom(ref _txMgr);
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        public override bool Init() {
            if ( ! base.Init()) return false;

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
            _terrain = new Terrain();
            _terrain.Init(Device, ImmediateContext, tii);

            _camera.Height = _terrain.Height;
            _txMgr = new TextureManager();
            _txMgr.Init(Device);
            _treeModel = new BasicModel(Device, _txMgr, "Models/tree.x", "Textures");
            _treeInstances = new List<BasicModelInstance>();
            
            
            for (var i = 0; i < NumTrees; i++) {
                var good = false;
                var x = MathF.Rand(0, _terrain.Width);
                var z = MathF.Rand(0, _terrain.Depth);
                while (!good) {

                    
                    if (_terrain.Height(x, z) < 12.0f) {
                        good = true;
                    }
                    x = MathF.Rand(-_terrain.Width/2, _terrain.Width/2);
                    z = MathF.Rand(-_terrain.Depth/2, _terrain.Depth/2);
                }
                var treeInstance = new BasicModelInstance {
                    Model = _treeModel,
                    World = Matrix.RotationX(MathF.PI / 2) * Matrix.Translation(x, _terrain.Height(x, z), z)
                };
                _treeInstances.Add(treeInstance);
            }
            BuildInstancedBuffer();

            return true;
        }

        private void BuildInstancedBuffer() {
            _instancedTrees = _treeInstances.Select(s => s.World).ToList();
            var bd = new BufferDescription(Marshal.SizeOf(typeof(Matrix)) * NumTrees, ResourceUsage.Dynamic, BindFlags.VertexBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
            _instanceBuffer = new Buffer(Device, new DataStream(_instancedTrees.ToArray(), false, true), bd);
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

            _visibleTrees = 0;
            var db = ImmediateContext.MapSubresource(_instanceBuffer, MapMode.WriteDiscard, MapFlags.None);
            foreach (var treeInstance in _treeInstances) {
                if (_camera.Visible(treeInstance.BoundingBox)) {
                    db.Data.Write(treeInstance.World);
                    _visibleTrees++;
                }
            }
            //MainWindowCaption = MainWindowCaption + _camera.Look.ToString();
            ImmediateContext.UnmapSubresource(_instanceBuffer, 0);

        }

        public override void DrawScene() {
            ImmediateContext.ClearRenderTargetView(RenderTargetView, Color.Silver);
            ImmediateContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);

            if (Util.IsKeyDown(Keys.W)) {
                ImmediateContext.Rasterizer.State = RenderStates.WireframeRS;
            }
            _terrain.Draw(ImmediateContext, _camera, _dirLights);

            ImmediateContext.InputAssembler.InputLayout = InputLayouts.InstancedPosNormalTexTan;
            ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            //var viewProj = _camera.ViewProj;

            Effects.InstancedNormalMapFX.SetDirLights(_dirLights);
            Effects.InstancedNormalMapFX.SetEyePosW(_camera.Position);
            /*
            var activeTech = Effects.InstancedNormalMapFX.Light3TexTech;
            for (int p = 0; p < activeTech.Description.PassCount; p++) {
                
                    Effects.InstancedNormalMapFX.SetViewProj(viewProj);
                    Effects.InstancedNormalMapFX.SetTexTransform(Matrix.Identity);

                    for (int i = 0; i < _treeModel.SubsetCount; i++) {
                        Effects.InstancedNormalMapFX.SetMaterial(_treeModel.Materials[i]);
                        Effects.InstancedNormalMapFX.SetDiffuseMap(_treeModel.DiffuseMapSRV[i]);
                        Effects.InstancedNormalMapFX.SetNormalMap(_treeModel.NormalMapSRV[i]);

                        activeTech.GetPassByIndex(p).Apply(ImmediateContext);
                        _treeModel.ModelMesh.DrawInstanced(ImmediateContext, i, _instanceBuffer, _visibleTrees, Marshal.SizeOf(typeof(Matrix)));
                    }
                
            }
            */
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

        static void Main() {
            Configuration.EnableObjectTracking = true;
            var app = new TerrainDemo(Process.GetCurrentProcess().Handle);
            if (!app.Init()) {
                return;
            }
            app.Run();

            
        }
    }
}
