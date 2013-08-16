using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreeBillboardDemo {
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Windows.Forms;

    using Core;
    using Core.FX;

    using SlimDX;
    using SlimDX.Direct3D11;
    using SlimDX.DXGI;

    using MapFlags = SlimDX.Direct3D11.MapFlags;

    public class TreeBillboardDemo : D3DApp {
        
        private Buffer _landVB;
        private Buffer _landIB;
        private Buffer _waveVB;
        private Buffer _waveIB;
        private Buffer _boxVB;
        private Buffer _boxIB;

        private Buffer _treeSpritesVB;

        private ShaderResourceView _grassMapSRV;
        private ShaderResourceView _wavesMapSRV;
        private ShaderResourceView _boxMapSRV;
        private ShaderResourceView _treeTextureMapArraySRV;

        private Waves _waves;

        private DirectionalLight[] _dirLights;
        private Material _landMat;
        private Material _wavesMat;
        private Material _boxMat;
        private Material _treeMat;

        private Matrix _grassTexTransform;
        private Matrix _waterTexTransform;
        private Matrix _landWorld;
        private Matrix _wavesWorld;
        private Matrix _boxWorld;

        private Matrix _view;
        private Matrix _proj;

        private int _landIndexCount;

        private const int TreeCount = 64;

        private bool _alphaToCoverageEnabled;

        private Vector2 _waterTexOffset;

        private RenderOptions _renderOptions;

        private Vector3 _eyePosW;

        private float _radius;
        private float _theta;
        private float _phi;

        private Point _lastMousePos;
        private float _tBase = 0;

        private bool _disposed;

        public TreeBillboardDemo(IntPtr hInstance) : base(hInstance) {
            _alphaToCoverageEnabled = true;
            _waterTexOffset = new Vector2();
            _eyePosW = new Vector3();
            _landIndexCount = 0;
            _renderOptions = RenderOptions.TexturesAndFog;
            _theta = 1.3f * MathF.PI;
            _phi = 0.4f * MathF.PI;
            _radius = 80.0f;

            MainWindowCaption = "Tree Billboard Demo";
            Enable4xMsaa = true;

            _lastMousePos = new Point();

            _landWorld = Matrix.Identity;
            _wavesWorld = Matrix.Translation(0, -3.0f, 0);
            _view = Matrix.Identity;
            _proj = Matrix.Identity;

            _boxWorld = Matrix.Scaling(15.0f, 15.0f, 15.0f) * Matrix.Translation(8.0f, 5.0f, -15.0f);

            _grassTexTransform = Matrix.Scaling(5, 5, 5);

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
                Diffuse = new Color4(0.6f, 1.0f, 1.0f, 1.0f),
                Specular = new Color4(32.0f, 0.8f, 0.8f, 0.8f)
            };
            _boxMat = new Material {
                Ambient = new Color4(0.5f, 0.5f, 0.5f),
                Diffuse = Color.White,
                Specular = new Color4(16.0f, 0.4f, 0.4f, 0.4f)
            };
            _treeMat = new Material {
                Ambient =  new Color4(0.5f, 0.5f, 0.5f),
                Diffuse = Color.White,
                Specular = new Color4(16.0f, 0.2f, 0.2f, 0.2f)
            };
        }
        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    ImmediateContext.ClearState();
                    Util.ReleaseCom(ref _landVB);
                    Util.ReleaseCom(ref _landIB);
                    Util.ReleaseCom(ref _waveVB);
                    Util.ReleaseCom(ref _waveIB);
                    Util.ReleaseCom(ref _boxVB);
                    Util.ReleaseCom(ref _boxIB);
                    Util.ReleaseCom(ref _treeSpritesVB);
                    Util.ReleaseCom(ref _grassMapSRV);
                    Util.ReleaseCom(ref _wavesMapSRV);
                    Util.ReleaseCom(ref _boxMapSRV);
                    Util.ReleaseCom(ref _treeTextureMapArraySRV);

                    Effects.DestroyAll();
                    InputLayouts.DestroyAll();
                    RenderStates.DestroyAll();
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        public override bool Init() {
            if (! base.Init()) return false;

            _waves = new Waves();
            _waves.Init(160, 160, 1.0f, 0.03f, 5.0f, 0.3f);

            Effects.InitAll(Device);
            InputLayouts.InitAll(Device);
            RenderStates.InitAll(Device);

            _grassMapSRV = ShaderResourceView.FromFile(Device, "Textures/grass.dds");
            _wavesMapSRV = ShaderResourceView.FromFile(Device, "Textures/water2.dds");
            _boxMapSRV = ShaderResourceView.FromFile(Device, "Textures/WireFence.dds");

            var filenames = new []{"Textures/tree0.dds", "Textures/tree1.dds", "Textures/tree2.dds", "Textures/tree3.dds"};

            _treeTextureMapArraySRV = Util.CreateTexture2DArraySRV(Device, ImmediateContext, filenames, Format.R8G8B8A8_UNorm);

            BuildLandGeometryBuffers();
            BuildWaveGeometryBuffers();
            BuildCrateGeometryBuffers();
            BuildTreeSpritesBuffers();

            Window.KeyDown += SwitchRenderState;

            return true;

        }
        private void SwitchRenderState(object sender, KeyEventArgs e) {
            switch (e.KeyCode) {
                case Keys.D1:
                    _renderOptions = RenderOptions.Lighting;
                    break;
                case Keys.D2:
                    _renderOptions = RenderOptions.Textures;
                    break;
                case Keys.D3:
                    _renderOptions = RenderOptions.TexturesAndFog;
                    break;
                case Keys.R:
                    _alphaToCoverageEnabled = true;
                    break;
                case Keys.T:
                    _alphaToCoverageEnabled = false;
                    break;
            }
        }

        public override void OnResize() {
            base.OnResize();
            _proj = Matrix.PerspectiveFovLH(0.25f * MathF.PI, AspectRatio, 1.0f, 1000.0f);
        }

        public override void UpdateScene(float dt) {
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
            if ((Timer.TotalTime - _tBase) >= 0.1f) {
                _tBase += 0.1f;

                var i = 5 + MathF.Rand() % (_waves.RowCount - 10);
                var j = 5 + MathF.Rand() % (_waves.ColumnCount - 10);
                var r = MathF.Rand(0.5f, 1.0f);
                _waves.Disturb(i, j, r);
            }
            _waves.Update(dt);
            // update waves vertex data
            var mappedData = ImmediateContext.MapSubresource(_waveVB, 0, MapMode.WriteDiscard, MapFlags.None);
            for (int i = 0; i < _waves.VertexCount; i++) {

                mappedData.Data.Write(new Basic32(
                    _waves[i],
                    _waves.Normal(i),
                    new Vector2(0.5f + _waves[i].X / _waves.Width, 0.5f - _waves[i].Z / _waves.Depth)));
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
            ImmediateContext.ClearRenderTargetView(RenderTargetView, Color.Silver);
            ImmediateContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);

            var blendFactor = new Color4(0, 0, 0, 0);

            var viewProj = _view * _proj;

            DrawTreeSprites(viewProj);
            

            ImmediateContext.InputAssembler.InputLayout = InputLayouts.Basic32;
            ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            Effects.BasicFX.SetDirLights(_dirLights);
            Effects.BasicFX.SetEyePosW(_eyePosW);

            Effects.BasicFX.SetFogColor(Color.Silver);
            Effects.BasicFX.SetFogStart(15.0f);
            Effects.BasicFX.SetFogRange(175.0f);

            EffectTechnique landAndWavesTech;
            EffectTechnique boxTech;
            switch (_renderOptions) {
                case RenderOptions.Lighting:
                    boxTech = Effects.BasicFX.Light3Tech;
                    landAndWavesTech = Effects.BasicFX.Light3Tech;
                    break;
                case RenderOptions.Textures:
                    boxTech = Effects.BasicFX.Light3TexAlphaClipTech;
                    landAndWavesTech = Effects.BasicFX.Light3TexTech;
                    break;
                case RenderOptions.TexturesAndFog:
                    boxTech = Effects.BasicFX.Light3TexAlphaClipFogTech;
                    landAndWavesTech = Effects.BasicFX.Light3TexFogTech;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            for (int p = 0; p < boxTech.Description.PassCount; p++) {
                ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_boxVB, Basic32.Stride, 0));
                ImmediateContext.InputAssembler.SetIndexBuffer(_boxIB, Format.R32_UInt, 0);

                var world = _boxWorld;
                var wit = MathF.InverseTranspose(world);
                var wvp = world * viewProj;

                Effects.BasicFX.SetWorld(world);
                Effects.BasicFX.SetWorldInvTranspose(wit);
                Effects.BasicFX.SetWorldViewProj(wvp);
                Effects.BasicFX.SetTexTransform(Matrix.Identity);
                Effects.BasicFX.SetMaterial(_boxMat);
                Effects.BasicFX.SetDiffuseMap(_boxMapSRV);

                ImmediateContext.Rasterizer.State = RenderStates.NoCullRS;
                boxTech.GetPassByIndex(p).Apply(ImmediateContext);
                ImmediateContext.DrawIndexed(36, 0, 0);

                ImmediateContext.Rasterizer.State = null;
            }


            for (int p = 0; p < landAndWavesTech.Description.PassCount; p++) {
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

                var pass = landAndWavesTech.GetPassByIndex(p);
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

                ImmediateContext.OutputMerger.BlendState = RenderStates.TransparentBS;
                ImmediateContext.OutputMerger.BlendFactor = blendFactor;
                ImmediateContext.OutputMerger.BlendSampleMask = ~0;
                pass.Apply(ImmediateContext);
                ImmediateContext.DrawIndexed(3 * _waves.TriangleCount, 0, 0);

                ImmediateContext.OutputMerger.BlendState = null;
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
                var dx = 0.1f * (e.X - _lastMousePos.X);
                var dy = 0.1f * (e.Y - _lastMousePos.Y);
                _radius += dx - dy;

                _radius = MathF.Clamp(_radius, 20.0f, 500.0f);
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
        private void BuildCrateGeometryBuffers() {
            var box = GeometryGenerator.CreateBox(1.0f, 1.0f, 1.0f);
            var vertices = new List<Basic32>();
            foreach (var v in box.Vertices) {
                vertices.Add(new Basic32(v.Position, v.Normal, v.TexC));
            }
            var vbd = new BufferDescription(Basic32.Stride * vertices.Count, ResourceUsage.Immutable, BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _boxVB = new Buffer(Device, new DataStream(vertices.ToArray(), false, false), vbd);

            var ibd = new BufferDescription(sizeof(int) * box.Indices.Count, ResourceUsage.Immutable, BindFlags.IndexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _boxIB = new Buffer(Device, new DataStream(box.Indices.ToArray(), false, false), ibd);

        }

        private void BuildTreeSpritesBuffers() {
            var v = new TreePointSprite[TreeCount];
            for (int i = 0; i < TreeCount; i++) {
                float y;
                float x;
                float z;
                do {
                    x = MathF.Rand(-75.0f, 75.0f);
                    z = MathF.Rand(-75.0f, 75.0f);
                    y = GetHillHeight(x, z);
                } while (y < -3 || y > 15.0f); 
                y += 10.0f;

                v[i].Pos = new Vector3(x,y,z);
                v[i].Size = new Vector2(24.0f,24.0f);
            }
            var vbd = new BufferDescription {
                Usage = ResourceUsage.Immutable,
                SizeInBytes = TreePointSprite.Stride * TreeCount,
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
            };
            _treeSpritesVB = new Buffer(Device, new DataStream(v, false, false), vbd);
        }

private void DrawTreeSprites(Matrix viewProj) {
    Effects.TreeSpriteFX.SetDirLights(_dirLights);
    Effects.TreeSpriteFX.SetEyePosW(_eyePosW);
    Effects.TreeSpriteFX.SetFogColor(Color.Silver);
    Effects.TreeSpriteFX.SetFogStart(15.0f);
    Effects.TreeSpriteFX.SetFogRange(175.0f);
    Effects.TreeSpriteFX.SetViewProj(viewProj);
    Effects.TreeSpriteFX.SetMaterial(_treeMat);
    Effects.TreeSpriteFX.SetTreeTextrueMapArray(_treeTextureMapArraySRV);

    ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.PointList;
    ImmediateContext.InputAssembler.InputLayout = InputLayouts.TreePointSprite;

    EffectTechnique treeTech;
    switch (_renderOptions) {
        case RenderOptions.Lighting:
            treeTech = Effects.TreeSpriteFX.Light3Tech;
            break;
        case RenderOptions.Textures:
            treeTech = Effects.TreeSpriteFX.Light3TexAlphaClipTech;
            break;
        case RenderOptions.TexturesAndFog:
            treeTech = Effects.TreeSpriteFX.Light3TexAlphaClipFogTech;
            break;
        default:
            throw new ArgumentOutOfRangeException();
    }
    for (int p = 0; p < treeTech.Description.PassCount; p++) {
        ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_treeSpritesVB, TreePointSprite.Stride, 0));

        var blendFactor = new Color4(0, 0, 0, 0);
        if (_alphaToCoverageEnabled) {
            ImmediateContext.OutputMerger.BlendState = RenderStates.AlphaToCoverageBS;
            ImmediateContext.OutputMerger.BlendFactor = blendFactor;
            ImmediateContext.OutputMerger.BlendSampleMask = -1;
        }
        treeTech.GetPassByIndex(p).Apply(ImmediateContext);
        ImmediateContext.Draw(TreeCount, 0);

        ImmediateContext.OutputMerger.BlendState = null;
        ImmediateContext.OutputMerger.BlendFactor = blendFactor;
        ImmediateContext.OutputMerger.BlendSampleMask = -1;

    }
}
    }


    class Program {
        static void Main(string[] args) {
            Configuration.EnableObjectTracking = true;
            var app = new TreeBillboardDemo(Process.GetCurrentProcess().Handle);
            if (!app.Init()) {
                return;
            }
            app.Run();
        }
    }
}
