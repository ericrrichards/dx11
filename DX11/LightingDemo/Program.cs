using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Core;
using SlimDX;
using SlimDX.D3DCompiler;
using SlimDX.DXGI;
using SlimDX.Direct3D11;
using Buffer = SlimDX.Direct3D11.Buffer;
using Debug = System.Diagnostics.Debug;
using Effect = SlimDX.Direct3D11.Effect;
using MapFlags = SlimDX.Direct3D11.MapFlags;

namespace LightingDemo {
    using Core.Vertex;

    public class LightingDemo : D3DApp {
        private Buffer _landVB;
        private Buffer _landIB;

        private Buffer _waveVB;
        private Buffer _waveIB;

        private Waves _waves;
        private DirectionalLight _dirLight;
        private PointLight _pointLight;
        private SpotLight _spotLight;
        private Material _landMaterial;
        private Material _wavesMaterial;

        private Effect _fx;
        private EffectTechnique _tech;
        private EffectMatrixVariable _fxWVP;
        private EffectMatrixVariable _fxWorld;
        private EffectMatrixVariable _fxWIT;
        private EffectVectorVariable _fxEyePosW;
        private EffectVariable _fxDirLight;
        private EffectVariable _fxPointLight;
        private EffectVariable _fxSpotLight;
        private EffectVariable _fxMaterial;

        private InputLayout _inputLayout;

        private Matrix _landWorld;
        private Matrix _wavesWorld;

        private Matrix _view;
        private Matrix _proj;

        private int _landIndexCount;

        private Vector3 _eyePosW;

        private float _theta;
        private float _phi;
        private float _radius;

        private Point _lastMousePos;


        private bool _disposed;
        private float _tBase;

        public LightingDemo(IntPtr hInstance) : base(hInstance) {
            _landVB = null;
            _landIB = null;
            _waveVB = null;
            _waveIB = null;
            _fx = null;
            _tech = null;
            _fxWorld = null;
            _fxWIT = null;
            _fxEyePosW = null;
            _fxDirLight = null;
            _fxPointLight = null;
            _fxSpotLight = null;
            _fxMaterial = null;
            _fxWVP = null;
            _inputLayout = null;
            _eyePosW = new Vector3();
            _theta = 1.5f*MathF.PI;
            _phi = 0.1f*MathF.PI;
            _radius = 80.0f;

            MainWindowCaption = "Lighting Demo";

            _lastMousePos = new Point();

            _landWorld = Matrix.Identity;
            _wavesWorld = Matrix.Translation(0, -3.0f, 0);
            _view = Matrix.Identity;
            _proj = Matrix.Identity;

            _dirLight = new DirectionalLight {
                Ambient = new Color4(0.2f, 0.2f, 0.2f),
                Diffuse = new Color4(0.5f, 0.5f, 0.5f),
                Specular = new Color4(0.5f, 0.5f, 0.5f),
                Direction = new Vector3(0.57735f, -0.57735f, 0.57735f)
            };

            _pointLight = new PointLight {
                Ambient = new Color4(0.3f, 0.3f, 0.3f),
                Diffuse = new Color4(0.7f, 0.7f, 0.7f),
                Specular = new Color4(0.7f, 0.7f, 0.7f),
                Attenuation = new Vector3(0.0f, 0.1f, 0.0f),
                Range = 25.0f
            };
            _spotLight = new SpotLight {
                Ambient = new Color4(0,0,0),
                Diffuse = new Color4(1.0f, 1.0f, 0.0f),
                Specular = Color.White,
                Attenuation = new Vector3(1.0f, 0.0f, 0.0f),
                Spot = 96.0f,
                Range = 10000.0f
            };

            // NOTE: must put alpha (spec power) first, rather than last as in book code
            _landMaterial = new Material {
                Ambient = new Color4(1.0f, 0.48f, 0.77f, 0.46f),
                Diffuse = new Color4(1.0f, 0.48f, 0.77f, 0.46f),
                Specular = new Color4(16.0f, 0.2f, 0.2f, 0.2f)
            };
            _wavesMaterial = new Material {
                Ambient =  new Color4(0.137f, 0.42f, 0.556f),
                Diffuse = new Color4(0.137f, 0.42f, 0.556f),
                Specular = new Color4(96.0f, 0.8f, 0.8f, 0.8f)
            };

        }
        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Util.ReleaseCom(ref _landVB);
                    Util.ReleaseCom(ref _landIB);
                    Util.ReleaseCom(ref _waveVB);
                    Util.ReleaseCom(ref _waveIB);

                    Util.ReleaseCom(ref _fx);
                    Util.ReleaseCom(ref _inputLayout);
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }
        public override bool Init() {
            if (!base.Init()) {
                return false;
            }

            _waves = new Waves();
            _waves.Init(160, 160, 1.0f, 0.03f, 3.25f, 0.4f);

            BuildLandGeometryBuffers();
            BuildWaveGeometryBuffers();
            BuildFX();
            BuildVertexLayout();

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
            _eyePosW = new Vector3(x,y,z);

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
                mappedData.Data.Write(new VertexPN(_waves[i], _waves.Normal(i)));
            }
            ImmediateContext.UnmapSubresource(_waveVB, 0);

            // animate lights
            _pointLight.Position = new Vector3(
                70.0f*MathF.Cos(0.2f*Timer.TotalTime), 
                Math.Max(GetHillHeight(_pointLight.Position.X, _pointLight.Position.Z), -3.0f) + 10.0f, 
                70.0f*MathF.Sin(0.2f*Timer.TotalTime)
            );
            _spotLight.Position = _eyePosW;
            _spotLight.Direction = Vector3.Normalize(target - pos);

        }
        public override void DrawScene() {
            base.DrawScene();
            ImmediateContext.ClearRenderTargetView(RenderTargetView, Color.LightSteelBlue);
            ImmediateContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);

            ImmediateContext.InputAssembler.InputLayout = _inputLayout;
            ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            var viewProj = _view*_proj;
            var array = Util.GetArray(_dirLight);
            _fxDirLight.SetRawValue(new DataStream(array, false, false), array.Length);
            array = Util.GetArray(_pointLight);
            _fxPointLight.SetRawValue(new DataStream(array, false, false), array.Length);
            array = Util.GetArray(_spotLight);
            _fxSpotLight.SetRawValue(new DataStream(array, false, false), array.Length );

            _fxEyePosW.Set(_eyePosW);
            
            for (int i = 0; i < _tech.Description.PassCount; i++) {
                ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_landVB, VertexPN.Stride, 0));
                ImmediateContext.InputAssembler.SetIndexBuffer(_landIB, Format.R32_UInt, 0);

                _fxWVP.SetMatrix(_landWorld * viewProj);
                var invTranspose = Matrix.Invert(Matrix.Transpose(_landWorld));
                _fxWIT.SetMatrix(invTranspose);
                _fxWorld.SetMatrix(_landWorld);
                array = Util.GetArray(_landMaterial);
                _fxMaterial.SetRawValue(new DataStream(array, false, false), array.Length);
                
                var pass = _tech.GetPassByIndex(i);
                pass.Apply(ImmediateContext);
                ImmediateContext.DrawIndexed(_landIndexCount, 0, 0);

                
                ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_waveVB, VertexPN.Stride, 0));
                ImmediateContext.InputAssembler.SetIndexBuffer(_waveIB, Format.R32_UInt, 0);

                _fxWVP.SetMatrix(_wavesWorld * viewProj);
                invTranspose = Matrix.Invert(Matrix.Transpose(_wavesWorld));
                _fxWIT.SetMatrix(invTranspose);
                _fxWorld.SetMatrix(_wavesWorld);
                array = Util.GetArray(_wavesMaterial);
                _fxMaterial.SetRawValue(new DataStream(array, false, false), array.Length);
                pass.Apply(ImmediateContext);
                ImmediateContext.DrawIndexed(3 * _waves.TriangleCount, 0, 0);
                

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

        private static float GetHillHeight(float x, float z) {
            return 0.3f * (z * MathF.Sin(0.1f * x) + x * MathF.Cos(0.1f * z));
        }
        private static Vector3 GetHillNormal(float x, float z) {
            var n = new Vector3(
                -0.03f*z*MathF.Cos(0.1f*x) - 0.3f*MathF.Cos(0.1f*z), 
                1.0f,
                -0.3f*MathF.Sin(0.1f*x) + 0.03f*x*MathF.Sin(0.1f*z)
                );
            n.Normalize();

            return n;
        }
        private void BuildLandGeometryBuffers() {
            var grid = GeometryGenerator.CreateGrid(160.0f, 160.0f, 50, 50);
            _landIndexCount = grid.Indices.Count;

            var vertices = new List<VertexPN>();
            foreach (var v in grid.Vertices) {
                var p = new Vector3(v.Position.X, GetHillHeight(v.Position.X, v.Position.Z), v.Position.Z);
                var n = GetHillNormal(p.X, p.Z);
                vertices.Add(new VertexPN(p,n));
            }
            var vbd = new BufferDescription(VertexPN.Stride * vertices.Count, ResourceUsage.Immutable, BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _landVB = new Buffer(Device, new DataStream(vertices.ToArray(), false, false), vbd);

            var ibd = new BufferDescription(sizeof(int) * grid.Indices.Count, ResourceUsage.Immutable, BindFlags.IndexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _landIB = new Buffer(Device, new DataStream(grid.Indices.ToArray(), false, false), ibd);
        }
        private void BuildWaveGeometryBuffers() {
            var vbd = new BufferDescription(VertexPN.Stride * _waves.VertexCount, ResourceUsage.Dynamic, BindFlags.VertexBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
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
        private void BuildFX() {
            ShaderBytecode compiledShader = null;
            try {
                compiledShader = new ShaderBytecode(new DataStream(File.ReadAllBytes("fx/Lighting.fxo"), false, false));
                _fx = new Effect(Device, compiledShader);
            } catch (Exception ex) {
                MessageBox.Show(ex.Message);
                return;
            } finally {
                Util.ReleaseCom(ref compiledShader);
            }

            _tech = _fx.GetTechniqueByName("LightTech");
            _fxWVP = _fx.GetVariableByName("gWorldViewProj").AsMatrix();
            _fxWorld = _fx.GetVariableByName("gWorld").AsMatrix();
            _fxWIT = _fx.GetVariableByName("gWorldInvTranspose").AsMatrix();
            _fxEyePosW = _fx.GetVariableByName("gEyePosW").AsVector();
            _fxDirLight = _fx.GetVariableByName("gDirLight");
            _fxPointLight = _fx.GetVariableByName("gPointLight");
            _fxSpotLight = _fx.GetVariableByName("gSpotLight");
            _fxMaterial = _fx.GetVariableByName("gMaterial");
        }
        private void BuildVertexLayout() {
            var vertexDesc = new[] {
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 
                    0, 0, InputClassification.PerVertexData, 0),
                new InputElement("NORMAL", 0, Format.R32G32B32_Float, 
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
            var app = new LightingDemo(Process.GetCurrentProcess().Handle);
            if (!app.Init()) {
                return;
            }
            app.Run();
        }
    }
}
