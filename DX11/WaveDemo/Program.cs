using System;
using System.Collections.Generic;

namespace WaveDemo {
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Windows.Forms;

    using Core;

    using SlimDX;
    using SlimDX.D3DCompiler;
    using SlimDX.DXGI;
    using SlimDX.Direct3D11;

    using Buffer = SlimDX.Direct3D11.Buffer;
    using Debug = System.Diagnostics.Debug;
    using MapFlags = SlimDX.Direct3D11.MapFlags;

    public class WaveDemo : D3DApp {
        private Buffer _landVB;
        private Buffer _landIB;
        private Buffer _wavesVB;
        private Buffer _wavesIB;

        private Effect _fx;
        private EffectTechnique _tech;
        private EffectMatrixVariable _fxWVP;

        private InputLayout _inputLayout;

        private RasterizerState _wireframeRS;

        private readonly Matrix _gridWorld;
        private readonly Matrix _wavesWorld;

        private int _gridIndexCount;
        private readonly Waves _waves;

        private Matrix _view;
        private Matrix _proj;

        private float _theta;
        private float _phi;
        private float _radius;
        private Point _lastMousePos;
        private float _tBase; 

        private bool _disposed;
        public WaveDemo(IntPtr hInstance) : base(hInstance) {
            _theta = 1.5f * MathF.PI;
            _phi = 0.1f * MathF.PI;
            _radius = 200.0f;
            
            MainWindowCaption = "Waves Demo";
            
            _lastMousePos = new Point(0,0);

            _gridWorld = Matrix.Identity;
            _wavesWorld = Matrix.Translation(0, -2.0f, 0);
            _view = Matrix.Identity;
            _proj = Matrix.Identity;

            _waves = new Waves();
        }
        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Util.ReleaseCom(_landVB);
                    Util.ReleaseCom(_landIB);
                    Util.ReleaseCom(_wavesVB);
                    Util.ReleaseCom(_wavesIB);
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

            _waves.Init(200, 200, 0.8f, 0.03f, 3.25f, 0.4f);

            BuildLandGeometryBuffers();
            BuildWavesGeometryBuffers();
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

            if ((Timer.TotalTime - _tBase) >= 0.25f) {
                _tBase += 0.25f;

                var i = 5 + MathF.Rand() % 190;
                var j = 5 + MathF.Rand() % 190;
                var r = MathF.Rand(1.0f, 2.0f);
                _waves.Disturb(i, j, r);
            }
            _waves.Update(dt);

            var mappedData = ImmediateContext.MapSubresource(_wavesVB, 0, MapMode.WriteDiscard, MapFlags.None);
            for (int i = 0; i < _waves.VertexCount; i++) {
                mappedData.Data.Write(new VertexPC(_waves[i], Color.Blue));
            }
            ImmediateContext.UnmapSubresource(_wavesVB, 0);
        }
        public override void DrawScene() {
            base.DrawScene();
            ImmediateContext.ClearRenderTargetView(RenderTargetView, Color.LightSteelBlue);
            ImmediateContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);

            ImmediateContext.InputAssembler.InputLayout = _inputLayout;
            ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            
            for (int i = 0; i < _tech.Description.PassCount; i++) {
                ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_landVB, VertexPC.Stride, 0));
                ImmediateContext.InputAssembler.SetIndexBuffer(_landIB, Format.R32_UInt, 0);

                _fxWVP.SetMatrix(_gridWorld * _view * _proj);
                var pass = _tech.GetPassByIndex(i);
                pass.Apply(ImmediateContext);
                ImmediateContext.DrawIndexed(_gridIndexCount, 0,0);

                ImmediateContext.Rasterizer.State = _wireframeRS;
                ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_wavesVB, VertexPC.Stride, 0));
                ImmediateContext.InputAssembler.SetIndexBuffer(_wavesIB, Format.R32_UInt, 0);

                _fxWVP.SetMatrix(_wavesWorld * _view * _proj);
                pass.Apply(ImmediateContext);
                ImmediateContext.DrawIndexed(3*_waves.TriangleCount, 0, 0);

                ImmediateContext.Rasterizer.State = null;
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
            return 0.3f * (z * MathF.Sin(0.1f * x) + x * MathF.Cos(0.1f * z));
        }

        private void BuildLandGeometryBuffers() {
            var grid = GeometryGenerator.CreateGrid(160.0f, 160.0f, 50, 50);
            var vertices = new List<VertexPC>();
            foreach (var vertex in grid.Vertices) {
                var pos = vertex.Position;
                pos.Y = GetHeight(pos.X, pos.Z);
                Color4 color;

                if (pos.Y < -10.0f) {
                    color = new Color4(1.0f, 1.0f, 0.96f, 0.62f);
                } else if (pos.Y < 5.0f) {
                    color = new Color4(1.0f, 0.48f, 0.77f, 0.46f);
                } else if (pos.Y < 12.0f) {
                    color = new Color4(1.0f, 0.1f, 0.48f, 0.19f);
                } else if (pos.Y < 20.0f) {
                    color = new Color4(1.0f, 0.45f, 0.39f, 0.34f);
                } else {
                    color = new Color4(1, 1, 1, 1);
                }
                vertices.Add(new VertexPC(pos, color));
            }
            var vbd = new BufferDescription(VertexPC.Stride * vertices.Count, ResourceUsage.Immutable, BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _landVB= new Buffer(Device, new DataStream(vertices.ToArray(), false, false), vbd);

            var ibd = new BufferDescription(sizeof(int) * grid.Indices.Count, ResourceUsage.Immutable, BindFlags.IndexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _landIB = new Buffer(Device, new DataStream(grid.Indices.ToArray(), false, false), ibd);
            _gridIndexCount = grid.Indices.Count;
        }
        private void BuildWavesGeometryBuffers() {
            var vbd = new BufferDescription(VertexPC.Stride * _waves.VertexCount, ResourceUsage.Dynamic, BindFlags.VertexBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
            _wavesVB = new Buffer(Device, vbd);

            var indices = new List<int>();
            var m = _waves.RowCount;
            var n = _waves.ColumnCount;
            for (int i = 0; i < m-1; i++) {
                for (int j = 0; j < n-1; j++) {
                    indices.Add(i*n+j);
                    indices.Add(i*n+j+1);
                    indices.Add((i+1)*n+j);

                    indices.Add((i + 1) * n + j);
                    indices.Add(i * n + j + 1);
                    indices.Add((i + 1) * n + j + 1);
                }
            }
            var ibd = new BufferDescription(sizeof(int) * indices.Count, ResourceUsage.Immutable, BindFlags.IndexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _wavesIB = new Buffer(Device, new DataStream(indices.ToArray(), false, false), ibd);
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
            var app = new WaveDemo(Process.GetCurrentProcess().Handle);
            if (!app.Init()) {
                return;
            }
            app.Run();
        }
    }
}
