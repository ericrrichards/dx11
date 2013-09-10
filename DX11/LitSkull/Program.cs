using System;
using System.Collections.Generic;
using System.Linq;

namespace LitSkull {
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Windows.Forms;

    using Core;
    using Core.FX;
    using Core.Vertex;

    using SlimDX;
    using SlimDX.DXGI;
    using SlimDX.Direct3D11;

    class LitSkullDemo : D3DApp {
        private Buffer _shapesVB;
        private Buffer _shapesIB;

        private Buffer _skullVB;
        private Buffer _skullIB;

        private readonly DirectionalLight[] _dirLights;
        private readonly Material _gridMat;
        private readonly Material _boxMat;
        private readonly Material _cylinderMat;
        private readonly Material _sphereMat;
        private readonly Material _skullMat;

        private readonly Matrix[] _sphereWorld = new Matrix[10];
        private readonly Matrix[] _cylWorld = new Matrix[10];
        private readonly Matrix _boxWorld;
        private readonly Matrix _gridWorld;
        private readonly Matrix _skullWorld;

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
        private int _skullIndexCount;

        private int _lightCount;

        private Vector3 _eyePosW;

        private float _theta;
        private float _phi;
        private float _radius;

        private Point _lastMousePos;


        private bool _disposed;
        private BasicEffect _fx;

        public LitSkullDemo(IntPtr hInstance)
            : base(hInstance) {
            _lightCount = 1;
            _eyePosW = new Vector3();
            _theta = 1.5f * MathF.PI;
            _phi = 0.1f * MathF.PI;
            _radius = 15.0f;

            MainWindowCaption = "Lit Skull Demo";

            _lastMousePos = new Point();

            _gridWorld = Matrix.Identity;
            _view = Matrix.Identity;
            _proj = Matrix.Identity;

            _boxWorld = Matrix.Scaling(3.0f, 1.0f, 3.0f) * Matrix.Translation(0, 0.5f, 0);

            _skullWorld = Matrix.Scaling(0.5f, 0.5f, 0.5f) * Matrix.Translation(0, 1.0f, 0);

            for (var i = 0; i < 5; i++) {
                _cylWorld[i * 2] = Matrix.Translation(-5.0f, 1.5f, -10.0f + i * 5.0f);
                _cylWorld[i * 2 + 1] = Matrix.Translation(5.0f, 1.5f, -10.0f + i * 5.0f);

                _sphereWorld[i * 2] = Matrix.Translation(-5.0f, 3.5f, -10.0f + i * 5.0f);
                _sphereWorld[i * 2 + 1] = Matrix.Translation(5.0f, 3.5f, -10.0f + i * 5.0f);
            }
            _dirLights = new[] {
                new DirectionalLight {
                    Ambient = new Color4( 0.2f, 0.2f, 0.2f),
                    Diffuse = new Color4(0.5f, 0.5f, 0.5f),
                    Specular = new Color4(0.5f, 0.5f, 0.5f),
                    Direction = new Vector3(0.57735f, -0.57735f, 0.57735f)
                },
                new DirectionalLight {
                    Ambient = new Color4(0,0,0),
                    Diffuse = new Color4(1.0f, 0.2f, 0.2f, 0.2f),
                    Specular = new Color4(1.0f, 0.25f, 0.25f, 0.25f),
                    Direction = new Vector3(-0.57735f, -0.57735f, 0.57735f)
                },
                new DirectionalLight {
                    Ambient = new Color4(0,0,0),
                    Diffuse = new Color4(1.0f,0.2f, 0.2f, 0.2f),
                    Specular = new Color4(0,0,0),
                    Direction = new Vector3(0, -0.707f, -0.707f)
                }
            };
            _gridMat = new Material {
                Ambient = new Color4(0.48f, 0.77f, 0.46f),
                Diffuse = new Color4(0.48f, 0.77f, 0.46f),
                Specular = new Color4(16.0f, 0.2f, 0.2f, 0.2f)
            };
            _cylinderMat = new Material {
                Ambient = new Color4(0.7f, 0.85f, 0.7f),
                Diffuse = new Color4(0.7f, 0.85f, 0.7f),
                Specular = new Color4(16.0f, 0.8f, 0.8f, 0.8f)
            };
            _sphereMat = new Material {
                Ambient = new Color4(0.1f, 0.2f, 0.3f),
                Diffuse = new Color4(0.2f, 0.4f, 0.6f),
                Specular = new Color4(16.0f, 0.9f, 0.9f, 0.9f)
            };
            _boxMat = new Material {
                Ambient = new Color4(0.651f, 0.5f, 0.392f),
                Diffuse = new Color4(0.651f, 0.5f, 0.392f),
                Specular = new Color4(16.0f, 0.2f, 0.2f, 0.2f)
            };
            _skullMat = new Material {
                Ambient = new Color4(0.8f, 0.8f, 0.8f),
                Diffuse = new Color4(0.8f, 0.8f, 0.8f),
                Specular = new Color4(16.0f, 0.8f, 0.8f, 0.8f)
            };

            

        }

        

        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Util.ReleaseCom(ref _shapesVB);
                    Util.ReleaseCom(ref _shapesIB);
                    Util.ReleaseCom(ref _skullVB);
                    Util.ReleaseCom(ref _skullIB);

                    Effects.DestroyAll();
                    InputLayouts.DestroyAll();
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }
        public override bool Init() {
            if (!base.Init()) {
                return false;
            }
            Effects.InitAll(Device);
            _fx =  Effects.BasicFX;
            InputLayouts.InitAll(Device);

            BuildShapeGeometryBuffers();
            BuildSkullGeometryBuffers();

            Window.KeyDown += SwitchLights;

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
            ImmediateContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth|DepthStencilClearFlags.Stencil, 1.0f, 0 );

            ImmediateContext.InputAssembler.InputLayout = InputLayouts.PosNormal;
            ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            var viewProj = _view * _proj;

            _fx.SetDirLights(_dirLights);
            _fx.SetEyePosW(_eyePosW);

            var activeTech = _fx.Light1Tech;
            switch (_lightCount) {
                case 1:
                    activeTech = _fx.Light1Tech;
                    break;
                case 2:
                    activeTech = _fx.Light2Tech;
                    break;
                case 3:
                    activeTech = _fx.Light3Tech;
                    break;
            }
            for (var p = 0; p < activeTech.Description.PassCount; p++) {
                var pass = activeTech.GetPassByIndex(p);
                ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_shapesVB, VertexPN.Stride, 0));
                ImmediateContext.InputAssembler.SetIndexBuffer(_shapesIB, Format.R32_UInt, 0);

                var world = _gridWorld;
                var worldInvTranspose = MathF.InverseTranspose(world);
                var wvp = world * viewProj;
                _fx.SetWorld(world);
                _fx.SetWorldInvTranspose(worldInvTranspose);
                _fx.SetWorldViewProj(wvp);
                _fx.SetMaterial(_gridMat);
                pass.Apply(ImmediateContext);
                ImmediateContext.DrawIndexed(_gridIndexCount, _gridIndexOffset, _gridVertexOffset);

                world = _boxWorld;
                worldInvTranspose = MathF.InverseTranspose(world);
                wvp = world * viewProj;
                _fx.SetWorld(world);
                _fx.SetWorldInvTranspose(worldInvTranspose);
                _fx.SetWorldViewProj(wvp);
                _fx.SetMaterial(_boxMat);
                pass.Apply(ImmediateContext);
                ImmediateContext.DrawIndexed(_boxIndexCount, _boxIndexOffset, _boxVertexOffset);

                foreach (var matrix in _cylWorld) {
                    world = matrix;
                    worldInvTranspose = MathF.InverseTranspose(world);
                    wvp = world * viewProj;
                    _fx.SetWorld(world);
                    _fx.SetWorldInvTranspose(worldInvTranspose);
                    _fx.SetWorldViewProj(wvp);
                    _fx.SetMaterial(_cylinderMat);
                    pass.Apply(ImmediateContext);
                    ImmediateContext.DrawIndexed(_cylinderIndexCount, _cylinderIndexOffset, _cylinderVertexOffset);
                }
                foreach (var matrix in _sphereWorld) {
                    world = matrix;
                    worldInvTranspose = MathF.InverseTranspose(world);
                    wvp = world * viewProj;
                    _fx.SetWorld(world);
                    _fx.SetWorldInvTranspose(worldInvTranspose);
                    _fx.SetWorldViewProj(wvp);
                    _fx.SetMaterial(_sphereMat);
                    pass.Apply(ImmediateContext);
                    ImmediateContext.DrawIndexed(_sphereIndexCount, _sphereIndexOffset, _sphereVertexOffset);
                }
                ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_skullVB, VertexPN.Stride, 0));
                ImmediateContext.InputAssembler.SetIndexBuffer(_skullIB, Format.R32_UInt, 0);

                world = _skullWorld;
                worldInvTranspose = MathF.InverseTranspose(world);
                wvp = world * viewProj;
                _fx.SetWorld(world);
                _fx.SetWorldInvTranspose(worldInvTranspose);
                _fx.SetWorldViewProj(wvp);
                _fx.SetMaterial(_skullMat);
                pass.Apply(ImmediateContext);
                ImmediateContext.DrawIndexed(_skullIndexCount, 0, 0);

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
        private void BuildShapeGeometryBuffers() {
            var box = GeometryGenerator.CreateBox(1, 1, 1);
            var grid = GeometryGenerator.CreateGrid(20, 30, 60, 40);
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

            var vertices = box.Vertices.Select(v => new VertexPN(v.Position, v.Normal)).ToList();
            vertices.AddRange(grid.Vertices.Select(v => new VertexPN(v.Position, v.Normal)));
            vertices.AddRange(sphere.Vertices.Select(v => new VertexPN(v.Position, v.Normal)));
            vertices.AddRange(cylinder.Vertices.Select(v => new VertexPN(v.Position, v.Normal)));

            var vbd = new BufferDescription(VertexPN.Stride * totalVertexCount, ResourceUsage.Immutable, BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _shapesVB = new Buffer(Device, new DataStream(vertices.ToArray(), false, false), vbd);

            var indices = new List<int>();
            indices.AddRange(box.Indices);
            indices.AddRange(grid.Indices);
            indices.AddRange(sphere.Indices);
            indices.AddRange(cylinder.Indices);

            var ibd = new BufferDescription(sizeof(int) * totalIndexCount, ResourceUsage.Immutable, BindFlags.IndexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _shapesIB = new Buffer(Device, new DataStream(indices.ToArray(), false, false), ibd);

        }
        private void BuildSkullGeometryBuffers() {
            try {
                var vertices = new List<VertexPN>();
                var indices = new List<int>();
                var vcount = 0;
                var tcount = 0;
                using (var reader = new StreamReader("Models\\skull.txt")) {


                    var input = reader.ReadLine();
                    if (input != null)
                        // VertexCount: X
                        vcount = Convert.ToInt32(input.Split(new[] { ':' })[1].Trim());

                    input = reader.ReadLine();
                    if (input != null)
                        //TriangleCount: X
                        tcount = Convert.ToInt32(input.Split(new[] { ':' })[1].Trim());

                    // skip ahead to the vertex data
                    do {
                        input = reader.ReadLine();
                    } while (input != null && !input.StartsWith("{"));
                    // Get the vertices  
                    for (int i = 0; i < vcount; i++) {
                        input = reader.ReadLine();
                        if (input != null) {
                            var vals = input.Split(new[] { ' ' });
                            vertices.Add(
                                new VertexPN(
                                    new Vector3(
                                        Convert.ToSingle(vals[0].Trim()),
                                        Convert.ToSingle(vals[1].Trim()),
                                        Convert.ToSingle(vals[2].Trim())),
                                    new Vector3(
                                        Convert.ToSingle(vals[3].Trim()), 
                                        Convert.ToSingle(vals[4].Trim()), 
                                        Convert.ToSingle(vals[5].Trim()))
                                )
                            );
                        }
                    }
                    // skip ahead to the index data
                    do {
                        input = reader.ReadLine();
                    } while (input != null && !input.StartsWith("{"));
                    // Get the indices
                    _skullIndexCount = 3 * tcount;
                    for (var i = 0; i < tcount; i++) {
                        input = reader.ReadLine();
                        if (input == null) {
                            break;
                        }
                        var m = input.Trim().Split(new[] { ' ' });
                        indices.Add(Convert.ToInt32(m[0].Trim()));
                        indices.Add(Convert.ToInt32(m[1].Trim()));
                        indices.Add(Convert.ToInt32(m[2].Trim()));
                    }
                }

                var vbd = new BufferDescription(VertexPN.Stride * vcount, ResourceUsage.Immutable,
                    BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
                _skullVB = new Buffer(Device, new DataStream(vertices.ToArray(), false, false), vbd);

                var ibd = new BufferDescription(sizeof(int) * _skullIndexCount, ResourceUsage.Immutable,
                    BindFlags.IndexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
                _skullIB = new Buffer(Device, new DataStream(indices.ToArray(), false, false), ibd);


            } catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }
        private void SwitchLights(object sender, KeyEventArgs e) {
            switch (e.KeyCode) {
                case Keys.D0:
                    _lightCount = 0;
                    break;
                case Keys.D1:
                    _lightCount = 1;
                    break;
                case Keys.D2:
                    _lightCount = 2;
                    break;
                case Keys.D3:
                    _lightCount = 3;
                    break;
            }
        }
    }

    class Program {
        static void Main(string[] args) {
            Configuration.EnableObjectTracking = true;
            var app = new LitSkullDemo(Process.GetCurrentProcess().Handle);
            if (!app.Init()) {
                return;
            }
            app.Run();
        }
    }
}
