using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrateDemo {
    using System.Diagnostics;
    using System.Drawing;
    using System.Windows.Forms;

    using Core.FX;

    using SlimDX.Direct3D11;
    using SlimDX.DXGI;

    using Buffer = SlimDX.Direct3D11.Buffer;

    using Core;

    using SlimDX;

    using ShaderResourceView = SlimDX.Direct3D11.ShaderResourceView;

    public class CrateDemo : D3DApp {
        private Buffer _boxVB;
        private Buffer _boxIB;

        private ShaderResourceView _diffuseMapSRV;

        private DirectionalLight[] _dirLights;
        private Material _boxMat;

        private Matrix _texTransform;
        private Matrix _boxWorld;
        private Matrix _view;
        private Matrix _proj;
        private int _boxVertexOffset;
        private int _boxIndexOffset;
        private int _boxIndexCount;

        private Vector3 _eyePosW;
        private float _theta;
        private float _phi;
        private float _radius;
        private Point _lastMousePos;

        private bool _disposed;
        private BasicEffect _fx;

        public CrateDemo(IntPtr hInstance) : base(hInstance) {
            MainWindowCaption = "Crate Demo";
            _eyePosW = new Vector3();
            _theta = 1.3f * MathF.PI;
            _phi = 0.4f * MathF.PI;
            _radius = 2.5f;

            _lastMousePos = new Point();

            _boxWorld = Matrix.Identity;
            _texTransform = Matrix.Identity;
            _view = Matrix.Identity;
            _proj = Matrix.Identity;

            _dirLights = new[] {
                new DirectionalLight {
                    Ambient = new Color4(0.3f, 0.3f, 0.3f),
                    Diffuse = new Color4(0.8f, 0.8f, 0.8f),
                    Specular = new Color4(16.0f, 0.6f, 0.6f, 0.6f),
                    Direction = new Vector3(0.707f, -0.707f, 0.0f)
                },
                new DirectionalLight {
                    Ambient = new Color4(0.2f, 0.2f, 0.2f),
                    Diffuse = new Color4(1.4f, 1.4f, 1.4f),
                    Specular = new Color4(16.0f, 0.3f, 0.3f, 0.3f),
                    Direction = new Vector3(-0.707f, 0, 0.707f)
                },
            };

            _boxMat = new Material {
                Ambient = new Color4(0.5f, 0.5f, 0.5f),
                Diffuse = new Color4(1,1,1),
                Specular = new Color4(16.0f, 0.6f, 0.6f, 0.6f)
            };

        }
        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Util.ReleaseCom(ref _boxVB);
                    Util.ReleaseCom(ref _boxIB);
                    Util.ReleaseCom(ref _diffuseMapSRV);
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
            _fx = Effects.BasicFX;
            InputLayouts.InitAll(Device);

            _diffuseMapSRV = ShaderResourceView.FromFile(Device, "Textures/WoodCrate01.dds") ;

            BuildGeometryBuffers();
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

            // Build the view matrix
            var pos = new Vector3(x, y, z);
            var target = new Vector3(0);
            var up = new Vector3(0, 1, 0);
            _view = Matrix.LookAtLH(pos, target, up);

            _eyePosW = pos;
        }

        public override void DrawScene() {
            ImmediateContext.ClearRenderTargetView(RenderTargetView, Color.LightSteelBlue);
            ImmediateContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);

            ImmediateContext.InputAssembler.InputLayout = InputLayouts.Basic32;
            ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            var viewProj = _view * _proj;

            _fx.SetDirLights(_dirLights);
            _fx.SetEyePosW(_eyePosW);

            var activeTech = _fx.Light2TexTech;

            for (int p = 0; p < activeTech.Description.PassCount; p++) {
                ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_boxVB, Basic32.Stride, 0));
                ImmediateContext.InputAssembler.SetIndexBuffer(_boxIB, Format.R32_UInt, 0);

                var world = _boxWorld;
                var wit = MathF.InverseTranspose(world);
                var wvp = world * viewProj;

                _fx.SetWorld(world);
                _fx.SetWorldInvTranspose(wit);
                _fx.SetWorldViewProj(wvp);
                _fx.SetTexTransform(_texTransform);
                _fx.SetMaterial(_boxMat);
                _fx.SetDiffuseMap( _diffuseMapSRV);

                activeTech.GetPassByIndex(p).Apply(ImmediateContext);

                ImmediateContext.DrawIndexed(_boxIndexCount, _boxIndexOffset, _boxVertexOffset);
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
                var dx = 0.01f * (e.X - _lastMousePos.X);
                var dy = 0.01f * (e.Y - _lastMousePos.Y);
                _radius += dx - dy;

                _radius = MathF.Clamp(_radius, 1.0f, 15.0f);
            }
            _lastMousePos = e.Location;
        }

        private void BuildGeometryBuffers() {
            var box = GeometryGenerator.CreateBox(1, 1, 1);

            _boxVertexOffset = 0;

            _boxIndexCount = box.Indices.Count;

            _boxIndexOffset = 0;

            var vertices = new List<Basic32>();

            foreach (var vertex in box.Vertices) {
                vertices.Add(new Basic32(vertex.Position, vertex.Normal, vertex.TexC));
            }
            var vbd = new BufferDescription(Basic32.Stride * vertices.Count, ResourceUsage.Immutable, BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _boxVB = new Buffer(Device, new DataStream(vertices.ToArray(), false, false), vbd);

            var indices = box.Indices;
            var ibd = new BufferDescription(sizeof(int) * indices.Count, ResourceUsage.Immutable, BindFlags.IndexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _boxIB = new Buffer(Device, new DataStream(indices.ToArray(), false, false), ibd);
        }
    }

    class Program {
        static void Main(string[] args) {
            Configuration.EnableObjectTracking = true;
            var app = new CrateDemo(Process.GetCurrentProcess().Handle);
            if (!app.Init()) {
                return;
            }
            app.Run();
        }
    }
}
