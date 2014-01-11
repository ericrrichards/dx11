using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Core;
using Core.Camera;
using Core.FX;
using Core.Model;
using Core.Terrain;
using SlimDX;
using SlimDX.Direct3D11;
using SlimDX.DXGI;

namespace _33_Pathfinding {
    class PathfindingDemo : D3DApp {
        private Sky _sky;
        private Terrain _terrain;
        private readonly DirectionalLight[] _dirLights;

        private readonly LookAtCamera _camera;

        private bool _camWalkMode;
        private Point _lastMousePos;
        private bool _disposed;

        private Ssao _ssao;


        private ShaderResourceView _whiteTex;

        private BoundingSphere _sceneBounds;

        private const int SMapSize = 4096;
        private const int MinimapSize = 512;
        private ShadowMap _sMap;
        private Matrix _lightView;
        private Matrix _lightProj;
        private Matrix _shadowTransform;
        private readonly Vector3[] _originalLightDirs;
        private float _lightRotationAngle;

        private Minimap _minimap;

        private BasicModel _sphereModel;
        private BasicModelInstance _sphere;
        private Unit _unit;

        private PathfindingDemo(IntPtr hInstance)
            : base(hInstance) {
            MainWindowCaption = "Pathfinding Demo";
            //Enable4xMsaa = true;
            _lastMousePos = new Point();

            _camera = new LookAtCamera {
                Position = new Vector3(0, 20, 100)
            };
            _dirLights = new[] {
                new DirectionalLight {
                    Ambient = new Color4(0.8f, 0.8f, 0.8f),
                    Diffuse = new Color4(0.6f, 0.6f, 0.5f),
                    Specular = new Color4(0.8f, 0.8f, 0.7f),
                    Direction = new Vector3(0, -.36f, 1.06f)
                },
                new DirectionalLight {
                    Ambient = new Color4(0,0,0),
                    Diffuse = new Color4( 0.4f, 0.4f, 0.4f),
                    Specular = new Color4( 0.2f, 0.2f, 0.2f),
                    Direction = new Vector3(0.707f, -0.707f, 0)
                },
                new DirectionalLight {
                    Ambient = new Color4(0,0,0),
                    Diffuse = new Color4(0.2f, 0.2f, 0.2f),
                    Specular = new Color4(0.2f,0.2f,0.2f),
                    Direction = new Vector3(0, 0, -1)
                }
            };
            _originalLightDirs = _dirLights.Select(l => l.Direction).ToArray();
        }

        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    ImmediateContext.ClearState();
                    Util.ReleaseCom(ref _sky);
                    Util.ReleaseCom(ref _terrain);
                    Util.ReleaseCom(ref _minimap);
                    Util.ReleaseCom(ref _sMap);
                    Util.ReleaseCom(ref _ssao);
                    Util.ReleaseCom(ref _whiteTex);
                    Util.ReleaseCom(ref _screenQuadIB);
                    Util.ReleaseCom(ref _screenQuadVB);
                    Util.ReleaseCom(ref _sphereModel);

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
                LayerMapFilename0 = "textures/grass.png",
                LayerMapFilename1 = "textures/hills.png",
                LayerMapFilename2 = "textures/stone.png",
                LayerMapFilename3 = "Textures/lightdirt.dds",
                LayerMapFilename4 = "textures/snow.png",
                Material = new Material() {
                    Ambient = Color.LightGray, Diffuse = Color.LightGray, Specular = new Color4(64, 0, 0, 0)
                },
                BlendMapFilename = null,
                HeightScale = 50.0f,
                HeightMapWidth = 2049,
                HeightMapHeight = 2049,
                CellSpacing = 0.5f,

                Seed = MathF.Rand(),
                NoiseSize1 = 3.0f,
                Persistence1 = 0.7f,
                Octaves1 = 7,
                NoiseSize2 = 2.5f,
                Persistence2 = 0.8f,
                Octaves2 = 3,


            };
            _terrain = new Terrain();
            //_terrain.DebugQuadTree = true;
            _terrain.Init(Device, ImmediateContext, tii);

            _camera.Height = _terrain.Height;


            _camera.SetLens(0.25f * MathF.PI, AspectRatio, 1.0f, 1000.0f);
            _ssao = new Ssao(Device, ImmediateContext, ClientWidth, ClientHeight, _camera.FovY, _camera.FarZ);

            _whiteTex = ShaderResourceView.FromFile(Device, "Textures/white.dds");

            _sMap = new ShadowMap(Device, SMapSize, SMapSize);

            _sceneBounds = new BoundingSphere(new Vector3(), MathF.Sqrt(_terrain.Width * _terrain.Width + _terrain.Depth * _terrain.Depth) / 2);

            _minimap = new Minimap(Device, ImmediateContext, MinimapSize, MinimapSize, _terrain, _camera);

            _sphereModel = new BasicModel();
            _sphereModel.CreateSphere(Device, 0.25f, 10, 10);
            _sphereModel.Materials[0] = new Material {
                Ambient = new Color4(63, 0, 0),
                Diffuse = Color.Red,
                Specular = new Color4(32, 1.0f, 1.0f, 1.0f)
            };
            _sphereModel.DiffuseMapSRV[0] = _whiteTex;

            _sphere = new BasicModelInstance(_sphereModel);

            _unit = new Unit(_sphere, _terrain.GetTile(511, 511), _terrain);

            return true;
        }



        public override void OnResize() {
            base.OnResize();
            _camera.SetLens(0.25f * MathF.PI, AspectRatio, 1.0f, 1000.0f);
            if (_ssao != null) {
                _ssao.OnSize(ClientWidth, ClientHeight, _camera.FovY, _camera.FarZ);
            }
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
            /*
            _lightRotationAngle += 0.1f * dt;

            var r = Matrix.RotationX(_lightRotationAngle);
            for (var i = 0; i < 3; i++) {
                var lightDir = _originalLightDirs[i];
                lightDir = Vector3.TransformNormal(lightDir, r);
                _dirLights[i].Direction = lightDir;
            }
            */
            BuildShadowTransform();

            _camera.UpdateViewMatrix();
            _unit.Update(dt);

        }


        private void BuildShadowTransform() {
            var lightDir = _dirLights[0].Direction;
            var lightPos = -2.0f * _sceneBounds.Radius * lightDir;
            var targetPos = _sceneBounds.Center;
            var up = new Vector3(0, 1, 0);

            var v = Matrix.LookAtLH(lightPos, targetPos, up);

            var sphereCenterLS = Vector3.TransformCoordinate(targetPos, v);

            var l = sphereCenterLS.X - _sceneBounds.Radius;
            var b = sphereCenterLS.Y - _sceneBounds.Radius;
            var n = sphereCenterLS.Z - _sceneBounds.Radius;
            var r = sphereCenterLS.X + _sceneBounds.Radius;
            var t = sphereCenterLS.Y + _sceneBounds.Radius;
            var f = sphereCenterLS.Z + _sceneBounds.Radius;


            //var p = Matrix.OrthoLH(r - l, t - b+5, n, f);
            var p = Matrix.OrthoOffCenterLH(l, r, b, t, n, f);
            var T = new Matrix {
                M11 = 0.5f,
                M22 = -0.5f,
                M33 = 1.0f,
                M41 = 0.5f,
                M42 = 0.5f,
                M44 = 1.0f
            };



            var s = v * p * T;
            _lightView = v;
            _lightProj = p;
            _shadowTransform = s;
        }

        public override void DrawScene() {
            Effects.TerrainFX.SetSsaoMap(_whiteTex);
            Effects.TerrainFX.SetShadowMap(_sMap.DepthMapSRV);
            Effects.TerrainFX.SetShadowTransform(_shadowTransform);
            _minimap.RenderMinimap(_dirLights);




            var view = _lightView;
            var proj = _lightProj;
            var viewProj = view * proj;

            _terrain.Renderer.DrawToShadowMap(ImmediateContext, _sMap, viewProj);

            ImmediateContext.Rasterizer.State = null;

            ImmediateContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);
            ImmediateContext.Rasterizer.SetViewports(Viewport);


            _terrain.Renderer.ComputeSsao(ImmediateContext, _camera, _ssao, DepthStencilView);


            ImmediateContext.OutputMerger.SetTargets(DepthStencilView, RenderTargetView);
            ImmediateContext.Rasterizer.SetViewports(Viewport);

            ImmediateContext.ClearRenderTargetView(RenderTargetView, Color.Silver);

            ImmediateContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);

            if (Util.IsKeyDown(Keys.W)) {
                ImmediateContext.Rasterizer.State = RenderStates.WireframeRS;
            }

            if (Util.IsKeyDown(Keys.S)) {
                Effects.TerrainFX.SetSsaoMap(_whiteTex);
            } else {

                Effects.TerrainFX.SetSsaoMap(_ssao.AmbientSRV);
            }
            if (!Util.IsKeyDown(Keys.A)) {
                Effects.TerrainFX.SetShadowMap(_sMap.DepthMapSRV);
                Effects.TerrainFX.SetShadowTransform(_shadowTransform);
            } else {
                Effects.TerrainFX.SetShadowMap(_whiteTex);
            }

            Effects.BasicFX.SetEyePosW(_camera.Position);
            Effects.BasicFX.SetDirLights(_dirLights);
            Effects.BasicFX.SetSsaoMap(_whiteTex);
            Effects.BasicFX.SetSsaoMap(_whiteTex);

            ImmediateContext.InputAssembler.InputLayout = InputLayouts.Basic32;
            ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            for (var p = 0; p < Effects.BasicFX.Light1Tech.Description.PassCount; p++) {
                var pass = Effects.BasicFX.Light1Tech.GetPassByIndex(p);
                _unit.Render(ImmediateContext, pass, _camera.View, _camera.Proj);

            }

            _terrain.Renderer.Draw(ImmediateContext, _camera, _dirLights);



            ImmediateContext.Rasterizer.State = null;




            _sky.Draw(ImmediateContext, _camera);


            ImmediateContext.Rasterizer.State = null;
            ImmediateContext.OutputMerger.DepthStencilState = null;
            ImmediateContext.OutputMerger.DepthStencilReference = 0;


            _minimap.Draw(ImmediateContext);
            SwapChain.Present(0, PresentFlags.None);


        }


        protected override void OnMouseDown(object sender, MouseEventArgs e) {
            switch (e.Button) {
                case MouseButtons.Left:
                    _minimap.OnClick(e);
                    _lastMousePos = e.Location;
                    Window.Capture = true;
                    break;
                case MouseButtons.Right:
                    // move the unit around using the right clicks
                    var ray = _camera.GetPickingRay(new Vector2(e.X, e.Y), new Vector2(Viewport.Width, Viewport.Height));

                    var tile = new MapTile();
                    var worldPos = new Vector3();

                    // do intersection test
                    if (!_terrain.Intersect(ray, ref worldPos, ref tile)) {
                        return;
                    }
                    Console.WriteLine("Clicked at " + worldPos.ToString());
                    if (tile == null) {
                        return;
                    }
                    // move the unit towards the new goal
                    Console.WriteLine("Hit tile " + tile.MapPosition);
                    Console.WriteLine("Moving unit to " + tile.MapPosition);
                    _unit.Goto(tile);

                    break;
            }
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

        private static void Main() {
            Configuration.EnableObjectTracking = true;
            var app = new PathfindingDemo(Process.GetCurrentProcess().Handle);
            if (!app.Init()) {
                return;
            }
            app.Run();
        }
    }
}
