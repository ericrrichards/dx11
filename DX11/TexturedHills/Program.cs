using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TexturedHills {
    using System.Diagnostics;
    using System.Drawing;
    using System.Windows.Forms;

    using Core;
    using Core.FX;

    using SlimDX;
    using SlimDX.Direct3D11;
    using SlimDX.DXGI;

    using MapFlags = SlimDX.Direct3D11.MapFlags;

    public class TexturedHillsDemo : D3DApp {
        private Buffer _landVB;
        private Buffer _landIB;

        private Buffer _waveVB;
        private Buffer _waveIB;

        private ShaderResourceView _grassMapSRV;
        private ShaderResourceView _wavesMapSRV;

        private Waves _waves;

        private DirectionalLight[] _dirLights;
        private Material _landMat;
        private Material _wavesMat;

        private Matrix _grassTexTransform;
        private Matrix _waterTexTransform;
        private Matrix _landWorld;
        private Matrix _wavesWorld;

        private Matrix _view;
        private Matrix _proj;

        private int _landIndexCount;
        private Vector2 _waterTexOffset;

        private Vector3 _eyePosW;
        
        private float _radius;
        private float _theta;
        private float _phi;

        private Point _lastMousePos;
        private bool _disposed;
        private float _tBase = 0;

        public TexturedHillsDemo(IntPtr hInstance) : base(hInstance) {
            _waterTexOffset = new Vector2();
            _eyePosW = new Vector3();
            _theta = 1.3f * MathF.PI;
            _phi = 0.4f * MathF.PI;
            _radius = 80.0f;

            MainWindowCaption = "Textured Hills and Waves Demo";

            _lastMousePos = new Point();

            _landWorld = Matrix.Identity;
            _wavesWorld = Matrix.Translation(0, -3.0f, 0);

            _view = Matrix.Identity;
            _proj = Matrix.Identity;
            
            _grassTexTransform = Matrix.Scaling(5.0f, 5.0f, 0.0f);

            _dirLights = new[] {
                new DirectionalLight {
                    Ambient = new Color4(0.2f, 0.2f, 0.2f),
                    Diffuse = new Color4(0.5f, 0.5f, 0.5f),
                    Specular = new Color4(0.5f, 0.5f, 0.5f),
                    Direction = new Vector3(0.57735f, -0.57735f, 0.57735f)
                }, 
                new DirectionalLight {
                    Ambient = Color.Black,
                    Diffuse = new Color4(0.2f, 0.2f, 0.2f),
                    Specular = new Color4(0.25f, 0.25f, 0.25f),
                    Direction = new Vector3(-0.57735f, -0.57735f, 0.57735f)
                }, 
                new DirectionalLight {
                    Ambient   = Color.Black,
                    Diffuse = new Color4(0.2f, 0.2f, 0.2f),
                    Specular = Color.Black,
                    Direction = new Vector3(0.0f, -0.707f, -0.707f)
                }
            };
            _landMat = new Material {
                Ambient = new Color4(0.5f, 0.5f, 0.5f),
                Diffuse = Color.White,
                Specular = new Color4(16.0f, 0.2f, 0.2f, 0.2f)
            };
            _wavesMat = new Material {
                Ambient = new Color4(0.5f, 0.5f, 0.5f),
                Diffuse = Color.White,
                Specular = new Color4(32.0f, 0.8f, 0.8f, 0.8f)
            };


        }

        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Util.ReleaseCom(ref _landVB);
                    Util.ReleaseCom(ref _landIB);
                    Util.ReleaseCom(ref _waveVB);
                    Util.ReleaseCom(ref _waveIB);
                    Util.ReleaseCom(ref _grassMapSRV);
                    Util.ReleaseCom(ref _wavesMapSRV);
                    Effects.DestroyAll();
                    InputLayouts.DestroyAll();
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        public override bool Init() {
            if (! base.Init()) return false;

            _waves = new Waves();
            _waves.Init(160, 160, 1.0f, 0.03f, 3.25f, 0.4f);

            Effects.InitAll(Device);
            InputLayouts.InitAll(Device);

            _grassMapSRV = ShaderResourceView.FromFile(Device, "Textures/grass.dds");
            _wavesMapSRV = ShaderResourceView.FromFile(Device, "Textures/water2.dds");

            BuildLandGeometryBuffers();
            BuildWaveGeometryBuffers();

            return true;
        }

        public override void OnResize() {
            base.OnResize();

            _proj = Matrix.PerspectiveFovLH(0.25f * MathF.PI, AspectRatio, 1.0f, 1000.0f);
        }

        public override void UpdateScene(float dt) {
            base.UpdateScene(dt);

            // Get camera position from polar coords
            var x = _radius * MathF.Sin(_phi) * MathF.Cos(_theta);
            var z = _radius * MathF.Sin(_phi) * MathF.Sin(_theta);
            var y = _radius * MathF.Cos(_phi);
            _eyePosW = new Vector3(x, y, z);

            // Build the view matrix
            var pos = new Vector3(x, y, z);
            var target = new Vector3(0);
            var up = new Vector3(0, 1, 0);
            _view = Matrix.LookAtLH(pos, target, up);

            // update waves
            if ((Timer.TotalTime - _tBase) >= 0.25f) {
                _tBase += 0.25f;

                var i = 5 + MathF.Rand() % (_waves.RowCount - 10);
                var j = 5 + MathF.Rand() % (_waves.ColumnCount - 10);
                var r = MathF.Rand(1.0f, 2.0f);
                _waves.Disturb(i, j, r);
            }
            _waves.Update(dt);
            // update waves vertex data
            var mappedData = ImmediateContext.MapSubresource(_waveVB, 0, MapMode.WriteDiscard, MapFlags.None);
            for (int i = 0; i < _waves.VertexCount; i++) {
               
                mappedData.Data.Write(new Basic32(
                    _waves[i], 
                    _waves.Normal(i), 
                    new Vector2(0.5f+_waves[i].X/_waves.Width, 0.5f - _waves[i].Z / _waves.Depth)));
            }
            ImmediateContext.UnmapSubresource(_waveVB, 0);

            // tile water texture
            var wavesScale = Matrix.Scaling(5.0f, 5.0f, 0.0f);
            // translate texture over time
            _waterTexOffset.Y += 0.05f * dt;
            _waterTexOffset.X += 0.1f * dt;
            var wavesOffset = Matrix.Translation(_waterTexOffset.X, _waterTexOffset.Y, 0);

            _waterTexTransform = wavesScale * wavesOffset;
        }

        public override void DrawScene() {
            base.DrawScene();
            ImmediateContext.ClearRenderTargetView(RenderTargetView, Color.LightSteelBlue);
            ImmediateContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);

            ImmediateContext.InputAssembler.InputLayout = InputLayouts.Basic32;
            ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            var viewProj = _view * _proj;

            Effects.BasicFX.SetDirLights(_dirLights);
            Effects.BasicFX.SetEyePosW(_eyePosW);

            var activeTech = Effects.BasicFX.Light3TexTech;

            for (int p = 0; p < activeTech.Description.PassCount; p++) {
                ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_landVB, Basic32.Stride, 0));
                ImmediateContext.InputAssembler.SetIndexBuffer(_landIB, Format.R32_UInt, 0);

                var world = _landWorld;
                var wit = MathF.InverseTranspose(world);
                var wvp = world * viewProj;

                Effects.BasicFX.SetWorld(world);
                Effects.BasicFX.SetWorldInvTranspose(wit);
                Effects.BasicFX.SetWorldViewProj(wvp);
                Effects.BasicFX.SetTexTransform(_grassTexTransform);
                Effects.BasicFX.SetMaterial(_landMat);
                Effects.BasicFX.SetDiffuseMap(_grassMapSRV);

                var pass = activeTech.GetPassByIndex(p);
                pass.Apply(ImmediateContext);
                ImmediateContext.DrawIndexed(_landIndexCount, 0, 0);

                ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_waveVB, Basic32.Stride, 0));
                ImmediateContext.InputAssembler.SetIndexBuffer(_waveIB, Format.R32_UInt, 0);

                world = _wavesWorld;
                wit = MathF.InverseTranspose(world);
                 wvp = world * viewProj;

                Effects.BasicFX.SetWorld(world);
                Effects.BasicFX.SetWorldInvTranspose(wit);
                Effects.BasicFX.SetWorldViewProj(wvp);
                Effects.BasicFX.SetTexTransform(_waterTexTransform);
                Effects.BasicFX.SetMaterial(_wavesMat);
                Effects.BasicFX.SetDiffuseMap(_wavesMapSRV);

                
                pass.Apply(ImmediateContext);
                ImmediateContext.DrawIndexed(3*_waves.TriangleCount, 0, 0);

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

                _theta += dx;
                _phi += dy;

                _phi = MathF.Clamp(_phi, 0.1f, MathF.PI - 0.1f);
            } else if (e.Button == MouseButtons.Right) {
                var dx = 0.05f * (e.X - _lastMousePos.X);
                var dy = 0.05f * (e.Y - _lastMousePos.Y);
                _radius += dx - dy;

                _radius = MathF.Clamp(_radius, 50.0f, 500.0f);
            }
            _lastMousePos = e.Location;
        }

        private static float GetHillHeight(float x, float z) {
            return 0.3f * (z * MathF.Sin(0.1f * x) + x * MathF.Cos(0.1f * z));
        }
        private static Vector3 GetHillNormal(float x, float z) {
            var n = new Vector3(
                -0.03f * z * MathF.Cos(0.1f * x) - 0.3f * MathF.Cos(0.1f * z),
                1.0f,
                -0.3f * MathF.Sin(0.1f * x) + 0.03f * x * MathF.Sin(0.1f * z)
                );
            n.Normalize();

            return n;
        }
        private void BuildLandGeometryBuffers() {
            var grid = GeometryGenerator.CreateGrid(160.0f, 160.0f, 50, 50);
            _landIndexCount = grid.Indices.Count;

            var vertices = new List<Basic32>();
            foreach (var v in grid.Vertices) {
                var p = new Vector3(v.Position.X, GetHillHeight(v.Position.X, v.Position.Z), v.Position.Z);
                var n = GetHillNormal(p.X, p.Z);
                vertices.Add(new Basic32(p, n, v.TexC));
            }
            var vbd = new BufferDescription(Basic32.Stride * vertices.Count, ResourceUsage.Immutable, BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _landVB = new Buffer(Device, new DataStream(vertices.ToArray(), false, false), vbd);

            var ibd = new BufferDescription(sizeof(int) * grid.Indices.Count, ResourceUsage.Immutable, BindFlags.IndexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _landIB = new Buffer(Device, new DataStream(grid.Indices.ToArray(), false, false), ibd);
        }
        private void BuildWaveGeometryBuffers() {
            var vbd = new BufferDescription(Basic32.Stride * _waves.VertexCount, ResourceUsage.Dynamic, BindFlags.VertexBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
            _waveVB = new Buffer(Device, vbd);

            var indices = new List<int>();
            var m = _waves.RowCount;
            var n = _waves.ColumnCount;
            for (int i = 0; i < m - 1; i++) {
                for (int j = 0; j < n - 1; j++) {
                    indices.Add(i * n + j);
                    indices.Add(i * n + j + 1);
                    indices.Add((i + 1) * n + j);

                    indices.Add((i + 1) * n + j);
                    indices.Add(i * n + j + 1);
                    indices.Add((i + 1) * n + j + 1);
                }
            }
            var ibd = new BufferDescription(sizeof(int) * indices.Count, ResourceUsage.Immutable, BindFlags.IndexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _waveIB = new Buffer(Device, new DataStream(indices.ToArray(), false, false), ibd);
        }
    }

    class Program {
        static void Main(string[] args) {
            Configuration.EnableObjectTracking = true;
            var app = new TexturedHillsDemo(Process.GetCurrentProcess().Handle);
            if (!app.Init()) {
                return;
            }
            app.Run();
        }
    }
}
