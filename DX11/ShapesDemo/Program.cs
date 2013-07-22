using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX;
using SlimDX.D3DCompiler;
using SlimDX.DXGI;
using SlimDX.Direct3D11;
using Debug = System.Diagnostics.Debug;

namespace ShapesDemo {
    using System.Windows.Forms;

    using Core;

    using SlimDX;

    using Buffer = SlimDX.Direct3D11.Buffer;

    class ShapesDemo : D3DApp {
        private Buffer _vb;
        private Buffer _ib;

        private Effect _fx;
        private EffectTechnique _tech;
        private EffectMatrixVariable _fxWVP;

        private InputLayout _inputLayout;

        private RasterizerState _wireframeRS;
        private Matrix[] _sphereWorld = new Matrix[10];
        private Matrix[] _cylWorld = new Matrix[10];
        private Matrix _boxWorld;
        private Matrix _gridWorld;
        private Matrix _centerSphere;

        private Matrix _view;
        private Matrix _proj;

        private int _boxVertexOffset;
        private int _gridVertexOffset;
        private int _sphereVertexOffset;
        private int _cylinderVertexOffset;

        private int _boxIndexOffset;
        private int _gridIndexOffset;
        private int _sphereIndexOffset;
        private int _cylinderIndexOffset;

        private int _boxIndexCount;
        private int _gridIndexCount;
        private int _sphereIndexCount;
        private int _cylinderIndexCount;

        private float _theta;
        private float _phi;
        private float _radius;
        private Point _lastMousePos;

        private bool _disposed;
        public ShapesDemo(IntPtr hInstance)
            : base(hInstance) {
            _vb = null;
            _ib = null;
            _fx = null;
            _tech = null;
            _fxWVP = null;
            _inputLayout = null;
            _wireframeRS = null;
            _theta = 1.5f * MathF.PI;
            _phi = 0.1f * MathF.PI;
            _radius = 15.0f;

            MainWindowCaption = "Shapes Demo";

            _lastMousePos = new Point(0, 0);

            _gridWorld = Matrix.Identity;
            _view = Matrix.Identity;
            _proj = Matrix.Identity;

            _boxWorld = Matrix.Scaling(2.0f, 1.0f, 2.0f) * Matrix.Translation(0, 0.5f, 0);
            _centerSphere = Matrix.Scaling(2.0f, 2.0f, 2.0f) * Matrix.Translation(0, 2, 0);

            for (int i = 0; i < 5; ++i) {
                _cylWorld[i * 2] = Matrix.Translation(-5.0f, 1.5f, -10.0f + i * 5.0f);
                _cylWorld[i * 2 + 1] = Matrix.Translation(5.0f, 1.5f, -10.0f + i * 5.0f);

                _sphereWorld[i * 2] = Matrix.Translation(-5.0f, 3.5f, -10.0f + i * 5.0f);
                _sphereWorld[i * 2 + 1] = Matrix.Translation(5.0f, 3.5f, -10.0f + i * 5.0f);
            }

        }
        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Util.ReleaseCom(_vb);
                    Util.ReleaseCom(_ib);
                    Util.ReleaseCom(_fx);
                    Util.ReleaseCom(_inputLayout);
                    Util.ReleaseCom(_wireframeRS);
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }
        public override bool Init() {
            if (!base.Init()) {
                return false;
            }
            BuildGeometryBuffers();
            BuildFX();
            BuildVertexLayout();

            var wireFrameDesc = new RasterizerStateDescription {
                FillMode = FillMode.Wireframe,
                CullMode = CullMode.Back,
                IsFrontCounterclockwise = false,
                IsDepthClipEnabled = true
            };

            _wireframeRS = RasterizerState.FromDescription(Device, wireFrameDesc);
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
        }
        public override void DrawScene() {
            base.DrawScene();
            ImmediateContext.ClearRenderTargetView(RenderTargetView, Color.LightSteelBlue);
            ImmediateContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);

            ImmediateContext.InputAssembler.InputLayout = _inputLayout;
            ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            ImmediateContext.Rasterizer.State = _wireframeRS;

            ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vb, VertexPC.Stride, 0));
            ImmediateContext.InputAssembler.SetIndexBuffer(_ib, Format.R32_UInt, 0);

            var viewProj = _view * _proj;

            var techDesc = _tech.Description;
            for (int p = 0; p < techDesc.PassCount; p++) {
                _fxWVP.SetMatrix(_gridWorld * viewProj);
                _tech.GetPassByIndex(p).Apply(ImmediateContext);
                ImmediateContext.DrawIndexed(_gridIndexCount, _gridIndexOffset, _gridVertexOffset);

                _fxWVP.SetMatrix(_boxWorld * viewProj);
                _tech.GetPassByIndex(p).Apply(ImmediateContext);
                ImmediateContext.DrawIndexed(_boxIndexCount, _boxIndexOffset, _boxVertexOffset);

                _fxWVP.SetMatrix(_centerSphere * viewProj);
                _tech.GetPassByIndex(p).Apply(ImmediateContext);
                ImmediateContext.DrawIndexed(_sphereIndexCount, _sphereIndexOffset, _sphereVertexOffset);

                foreach (var matrix in _cylWorld) {
                    _fxWVP.SetMatrix(matrix * viewProj);
                    _tech.GetPassByIndex(p).Apply(ImmediateContext);
                    ImmediateContext.DrawIndexed(_cylinderIndexCount, _cylinderIndexOffset, _cylinderVertexOffset);
                }
                foreach (var matrix in _sphereWorld) {
                    _fxWVP.SetMatrix(matrix * viewProj);
                    _tech.GetPassByIndex(p).Apply(ImmediateContext);
                    ImmediateContext.DrawIndexed(_sphereIndexCount, _sphereIndexOffset, _sphereVertexOffset);
                }
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

                _radius = MathF.Clamp(_radius, 3.0f, 200.0f);
            }
            _lastMousePos = e.Location;
        }

        private void BuildGeometryBuffers() {
            var box = GeometryGenerator.CreateBox(1.0f, 1.0f, 1.0f);
            var grid = GeometryGenerator.CreateGrid(20.0f, 30.0f, 60, 40);
            var sphere = GeometryGenerator.CreateSphere(0.5f, 20, 20);
            var cylinder = GeometryGenerator.CreateCylinder(0.5f, 0.3f, 3.0f, 20, 20);

            _boxVertexOffset = 0;
            _gridVertexOffset = box.Vertices.Count;
            _sphereVertexOffset = _gridVertexOffset + grid.Vertices.Count;
            _cylinderVertexOffset = _sphereVertexOffset + sphere.Vertices.Count;

            _boxIndexCount = box.Indices.Count;
            _gridIndexCount = grid.Indices.Count;
            _sphereIndexCount = sphere.Indices.Count;
            _cylinderIndexCount = cylinder.Indices.Count;

            _boxIndexOffset = 0;
            _gridIndexOffset = _boxIndexCount;
            _sphereIndexOffset = _gridIndexOffset + _gridIndexCount;
            _cylinderIndexOffset = _sphereIndexOffset + _sphereIndexCount;

            var totalVertexCount = box.Vertices.Count + grid.Vertices.Count + sphere.Vertices.Count + cylinder.Vertices.Count;
            var totalIndexCount = _boxIndexCount + _gridIndexCount + _sphereIndexCount + _cylinderIndexCount;

            var vs = new List<VertexPC>();
            foreach (var vertex in box.Vertices) {
                vs.Add(new VertexPC(vertex.Position, Color.Black));
            }
            foreach (var v in grid.Vertices) {
                vs.Add(new VertexPC(v.Position, Color.Black));
            }
            foreach (var v in sphere.Vertices) {
                vs.Add(new VertexPC(v.Position, Color.Black));
            }
            foreach (var v in cylinder.Vertices) {
                vs.Add(new VertexPC(v.Position, Color.Black));
            }
            var vbd = new BufferDescription(VertexPC.Stride * totalVertexCount,
                ResourceUsage.Immutable, BindFlags.VertexBuffer,
                CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _vb = new Buffer(Device, new DataStream(vs.ToArray(), false, false), vbd);

            var indices = new List<int>();
            indices.AddRange(box.Indices);
            indices.AddRange(grid.Indices);
            indices.AddRange(sphere.Indices);
            indices.AddRange(cylinder.Indices);

            var ibd = new BufferDescription(sizeof(int) * totalIndexCount, ResourceUsage.Immutable,
                BindFlags.IndexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _ib = new Buffer(Device, new DataStream(indices.ToArray(), false, false), ibd);

        }
        private void BuildFX() {
            ShaderBytecode compiledShader = null;
            try {
                compiledShader = new ShaderBytecode(new DataStream(File.ReadAllBytes("fx/color.fxo"), false, false));
                _fx = new Effect(Device, compiledShader);
            } catch (Exception ex) {
                MessageBox.Show(ex.Message);
                return;
            } finally {
                Util.ReleaseCom(compiledShader);
            }

            _tech = _fx.GetTechniqueByName("ColorTech");
            _fxWVP = _fx.GetVariableByName("gWorldViewProj").AsMatrix();
        }
        private void BuildVertexLayout() {
            var vertexDesc = new[] {
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 
                    0, 0, InputClassification.PerVertexData, 0),
                new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 
                    12, 0, InputClassification.PerVertexData, 0)
            };
            Debug.Assert(_tech != null);
            var passDesc = _tech.GetPassByIndex(0).Description;
            _inputLayout = new InputLayout(Device, passDesc.Signature, vertexDesc);
        }

    }

    class Program {
        static void Main(string[] args) {
            Configuration.EnableObjectTracking = true;
            var app = new ShapesDemo(Process.GetCurrentProcess().Handle);
            if (!app.Init()) {
                return;
            }
            app.Run();
        }
    }
}
