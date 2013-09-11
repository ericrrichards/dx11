using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Core;
using Core.Camera;
using Core.FX;
using Core.Vertex;
using SlimDX;
using SlimDX.DXGI;
using SlimDX.Direct3D11;
using Buffer = SlimDX.Direct3D11.Buffer;

namespace VoronoiMap {
    using System.Linq;

    class VoronoiMapDemo :D3DApp {
        private FpsCamera _camera;
        private Point _lastMousePos;
        private Map _map;
        private Buffer _mapVB;
        private Buffer _mapIB;


        private bool _disposed;
        private Dictionary<string, Color4> _biomeColors;
        private int _mapVertCount;
        

        protected VoronoiMapDemo(IntPtr hInstance) : base(hInstance) {
            MainWindowCaption = "Voronoi Map Demo";
            _lastMousePos = new Point();

            _biomeColors = new Dictionary<string, Color4>();
            _biomeColors["Ocean"] = Color.DarkBlue;
            _biomeColors["Marsh"] = Color.CadetBlue;
            _biomeColors["Ice"] = Color.AliceBlue;
            _biomeColors["Lake"] = Color.Blue;
            _biomeColors["Beach"] = Color.Gold;
            _biomeColors["Snow"] = Color.White;
            _biomeColors["Tundra"] = Color.OliveDrab;
            _biomeColors["Bare"] = Color.DarkGray;
            _biomeColors["Scorched"] = Color.DarkRed;
            _biomeColors["Taiga"] = Color.DarkGreen;
            _biomeColors["Shrubland"] = Color.DarkOliveGreen;
            _biomeColors["TemperateDesert"] = Color.SaddleBrown;
            _biomeColors["TemperateRainForest"] = Color.ForestGreen;
            _biomeColors["TemperateDecidousForest"] = Color.DarkSeaGreen;
            _biomeColors["Grassland"] = Color.YellowGreen;
            _biomeColors["TropicalRainForest"] = Color.LawnGreen;
            _biomeColors["TropicalSeasonalForest"] = Color.Olive;
            _biomeColors["SubtropicalDesert"] = Color.Khaki; 

            _camera = new FpsCamera { Position = new Vector3(0, 2, -15) };
            _map = new Map(100.0f);
            _map.NewIsland("square", 0, 0);
            _map.Go(0,0);
        }

        
        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Util.ReleaseCom(ref _mapVB);
                    Util.ReleaseCom(ref _mapIB);
                    RenderStates.DestroyAll();
                    Effects.DestroyAll();
                    InputLayouts.DestroyAll();
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

            BuildMapGeomertryBuffers();
            return true;
        }
        public override void OnResize() {
            base.OnResize();
            _camera.SetLens(0.25f * MathF.PI, AspectRatio, 1.0f, 1000.0f);
        }

        public override void UpdateScene(float dt) {
            base.UpdateScene(dt);
            if (Util.IsKeyDown(Keys.Up)) {
                _camera.Walk(100.0f * dt);
            }
            if (Util.IsKeyDown(Keys.Down)) {
                _camera.Walk(-100.0f * dt);
            }
            if (Util.IsKeyDown(Keys.Left)) {
                _camera.Strafe(-100.0f * dt);
            }
            if (Util.IsKeyDown(Keys.Right)) {
                _camera.Strafe(100.0f * dt);
            }
            if (Util.IsKeyDown(Keys.PageUp)) {
                _camera.Zoom(-dt);
            }
            if (Util.IsKeyDown(Keys.PageDown)) {
                _camera.Zoom(+dt);
            }
            if (Util.IsKeyDown(Keys.Space)) {
                _camera.LookAt(new Vector3(0, 5, -5), new Vector3(0,0,0), Vector3.UnitY);
            }
        }
        public override void DrawScene() {

            ImmediateContext.ClearRenderTargetView(RenderTargetView, Color.Silver);
            ImmediateContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);

            ImmediateContext.InputAssembler.InputLayout = InputLayouts.PosColor;
            ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            _camera.UpdateViewMatrix();

            Matrix view = _camera.View;
            Matrix proj = _camera.Proj;
            Matrix viewProj = _camera.ViewProj;
            Effects.ColorFX.SetWorldViewProj(viewProj);

            ImmediateContext.Rasterizer.State = RenderStates.NoCullRS;
            
            if (Util.IsKeyDown(Keys.W)) {
                ImmediateContext.Rasterizer.State = RenderStates.WireframeRS;
            }
            ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_mapVB, VertexPC.Stride, 0));

            for (int p = 0; p < Effects.ColorFX.ColorTech.Description.PassCount; p++) {
                var pass = Effects.ColorFX.ColorTech.GetPassByIndex(p);
                pass.Apply(ImmediateContext);
                ImmediateContext.Draw(_mapVertCount, 0);
            }
            ImmediateContext.Rasterizer.State = null;

            

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
        private void BuildMapGeomertryBuffers() {
            var verts = new List<VertexPC>();
            var indices = new List<int>();
            foreach (var p in _map.Centers) {
                if ( p.Ocean) continue;
                foreach (var edge in p.Borders) {
                    var c1 = edge.V0;
                    var c2 = edge.V1;
                    var c3 = edge.Midpoint;
                    if ( c1 == null || c2 == null ) continue;

                    verts.Add(new VertexPC(new Vector3(p.Point.X, p.Elevation * 2.0f, p.Point.Y), _biomeColors[p.Biome]));
                    verts.Add(new VertexPC(new Vector3(c1.Point.X, c1.Elevation*2.0f, c1.Point.Y), _biomeColors[p.Biome] ));
                    verts.Add(new VertexPC(new Vector3(c3.Value.X, (c1.Elevation + c2.Elevation)*1.0f, c3.Value.Y), _biomeColors[p.Biome] ));
                    verts.Add(new VertexPC(new Vector3(p.Point.X, p.Elevation * 2.0f, p.Point.Y), _biomeColors[p.Biome]));
                    verts.Add(new VertexPC(new Vector3(c3.Value.X, (c1.Elevation + c2.Elevation) *1.0f, c3.Value.Y), _biomeColors[p.Biome]));
                    verts.Add(new VertexPC(new Vector3(c2.Point.X, c2.Elevation * 2.0f, c2.Point.Y), _biomeColors[p.Biome]));
                    
                }
            }
            _mapVertCount = verts.Count;
            var vbd = new BufferDescription(VertexPC.Stride*verts.Count, ResourceUsage.Immutable, BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _mapVB = new Buffer(Device, new DataStream(verts.ToArray(), false, false), vbd);
        }

        static void Main(string[] args) {
            Configuration.EnableObjectTracking = true;
            var app = new VoronoiMapDemo(Process.GetCurrentProcess().Handle);
            if (!app.Init()) {
                return;
            }
            app.Run();
             
        }
        
    }


    
}
