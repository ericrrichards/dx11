using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Core;
using SlimDX;
using SlimDX.D3DCompiler;
using SlimDX.DXGI;
using SlimDX.Direct3D11;
using Buffer = SlimDX.Direct3D11.Buffer;
using Debug = System.Diagnostics.Debug;

namespace HillDemo {
    public class BoxApp : D3DApp {
        private Buffer _vb;
        private Buffer _ib;

        private Effect _fx;
        private EffectTechnique _tech;
        private EffectMatrixVariable _fxWVP;

        private InputLayout _inputLayout;

        private int _gridIndexCount;

        // Matrices
        private Matrix _world;
        private Matrix _view;
        private Matrix _proj;

        // Camera variables
        private float _theta;
        private float _phi;
        private float _radius;

        private Point _lastMousePos;

        private bool _disposed;

        public BoxApp(IntPtr hInstance)
            : base(hInstance) {
            _ib = null;
            _vb = null;
            _fx = null;
            _tech = null;
            _fxWVP = null;
            _inputLayout = null;
            _theta = 1.5f * MathF.PI;
            _phi = 0.1f * MathF.PI;
            _radius = 200.0f;

            MainWindowCaption = "Hills Demo";
            _lastMousePos = new Point(0, 0);
            _world = Matrix.Identity;
            _view = Matrix.Identity;
            _proj = Matrix.Identity;

            _gridIndexCount = 0;
        }
        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Util.ReleaseCom(_vb);
                    Util.ReleaseCom(_ib);
                    Util.ReleaseCom(_fx);
                    Util.ReleaseCom(_inputLayout);
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

            return true;
        }
        public override void OnResize() {
            base.OnResize();
            // Recalculate perspective matrix
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

            ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vb, Vertex.Stride, 0));
            ImmediateContext.InputAssembler.SetIndexBuffer(_ib, Format.R32_UInt, 0);

            var wvp = _world * _view * _proj;
            

            for (int p = 0; p < _tech.Description.PassCount; p++) {
                _fxWVP.SetMatrix(wvp);
                _tech.GetPassByIndex(p).Apply(ImmediateContext);
                ImmediateContext.DrawIndexed(_gridIndexCount, 0, 0);
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
                var dx = 0.2f * (e.X - _lastMousePos.X);
                var dy = 0.2f * (e.Y - _lastMousePos.Y);
                _radius += dx - dy;

                _radius = MathF.Clamp(_radius, 50.0f, 500.0f);
            }
            _lastMousePos = e.Location;
        }

        private float GetHeight(float x, float z) {
            return 0.3f*(z*MathF.Sin(0.1f*x) + x*MathF.Cos(0.1f*z));
        }

        private void BuildGeometryBuffers() {
            var grid = GeometryGenerator.CreateGrid(160.0f, 160.0f, 50, 50);
            var vertices = new List<Vertex>();
            foreach (var vertex in grid.Vertices) {
                var pos = vertex.Position;
                pos.Y = GetHeight(pos.X, pos.Z);
                Color4 color;

                if (pos.Y < -10.0f) {
                    color = new Color4(1.0f, 0.96f, 0.62f, 1.0f);
                } else if (pos.Y < 5.0f) {
                    color = new Color4(1.0f, 0.48f, 0.77f, 0.46f);
                } else if (pos.Y < 12.0f) {
                    color = new Color4(1.0f, 0.1f, 0.48f, 0.19f);
                } else if (pos.Y < 20.0f) {
                    color = new Color4(1.0f, 0.45f, 0.39f, 0.34f);
                } else {
                    color = new Color4(1,1,1,1);
                }
                vertices.Add(new Vertex(pos, color));
            }
            var vbd = new BufferDescription(Vertex.Stride, ResourceUsage.Immutable, BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _vb = new Buffer(Device, new DataStream(vertices.ToArray(), false, false), vbd);

            var ibd = new BufferDescription(sizeof (int)*grid.Indices.Count, ResourceUsage.Immutable, BindFlags.IndexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _ib = new Buffer(Device, new DataStream(grid.Indices.ToArray(), false, false), ibd);


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


    static class Program {
        static void Main(string[] args) {
            Configuration.EnableObjectTracking = true;
            var app = new BoxApp(Process.GetCurrentProcess().Handle);
            if (!app.Init()) {
                return;
            }
            app.Run();
        }
    }
}
