using System;
using System.Collections.Generic;

namespace PickingDemo {
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Windows.Forms;

    using Core;
    using Core.Camera;
    using Core.FX;
    using Core.Vertex;

    using SlimDX;
    using SlimDX.DXGI;
    using SlimDX.Direct3D11;

    class PickingDemo : D3DApp {
        private Buffer _meshVB;
        private Buffer _meshIB;
        private List<Basic32> _meshVertices;
        private List<int> _meshIndices;
        private BoundingBox _meshBox;
        private readonly DirectionalLight[] _dirLights;
        private readonly Material _meshMat;
        private readonly Material _pickedTriangleMat;
        private readonly Matrix _meshWorld;
        private int _meshIndexCount;
        private int _pickedTriangle;
        private readonly FpsCamera _cam;
        private Point _lastMousePos;
        private bool _disposed;
        
        public PickingDemo(IntPtr hInstance) : base(hInstance) {
            _pickedTriangle = -1;
            MainWindowCaption = "Picking Demo";

            _lastMousePos = new Point();

            _cam = new FpsCamera {
                Position = new Vector3(0, 2, -15)
            };
            _meshWorld = Matrix.Scaling(0.5f, 0.5f, 0.5f) * Matrix.Translation(0, 1, 0);
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
            _meshMat = new Material {
                Ambient = new Color4(0.4f, 0.4f, 0.4f),
                Diffuse = new Color4(0.8f, 0.8f, 0.8f),
                Specular = new Color4(16.0f, 0.8f, 0.8f, 0.8f)
            };
            _pickedTriangleMat = new Material {
                Ambient = new Color4(0, 0.8f, 0.4f),
                Diffuse = new Color4(0, 0.8f, 0.4f),
                Specular = new Color4(16.0f, 0.0f, 0.0f, 0.0f)
            };

        }
        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Util.ReleaseCom(ref _meshVB);
                    Util.ReleaseCom(ref _meshIB);
                    Effects.DestroyAll();
                    InputLayouts.DestroyAll();
                    RenderStates.DestroyAll();
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


            BuildMeshGeometryBuffers();
            return true;
        }
        public override void OnResize() {
            base.OnResize();
            _cam.SetLens(0.25f* MathF.PI, AspectRatio, 1.0f, 1000.0f);
        }
        public override void UpdateScene(float dt) {
            base.UpdateScene(dt);
            if (Util.IsKeyDown(Keys.Up)) {
                _cam.Walk(10.0f*dt);
            }
            if (Util.IsKeyDown(Keys.Down)) {
                _cam.Walk(-10.0f*dt);
            }
            if (Util.IsKeyDown(Keys.Left)) {
                _cam.Strafe(-10.0f*dt);
            }
            if (Util.IsKeyDown(Keys.Right)) {
                _cam.Strafe(10.0f*dt);
            }
        }
        public override void DrawScene() {
            base.DrawScene();
            ImmediateContext.ClearRenderTargetView(RenderTargetView, Color.Silver);
            ImmediateContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth|DepthStencilClearFlags.Stencil, 1.0f, 0 );

            ImmediateContext.InputAssembler.InputLayout = InputLayouts.Basic32;
            ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            var stride = Basic32.Stride;
            const int offset = 0;

            _cam.UpdateViewMatrix();

            
            var viewProj = _cam.ViewProj;

            Effects.BasicFX.SetDirLights(_dirLights);
            Effects.BasicFX.SetEyePosW(_cam.Position);

            var activeTech = Effects.BasicFX.Light3Tech;

            for (int p = 0; p < activeTech.Description.PassCount; p++) {
                if (Util.IsKeyDown(Keys.D1)) {
                    ImmediateContext.Rasterizer.State = RenderStates.WireframeRS;
                }
                ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_meshVB, stride, offset));
                ImmediateContext.InputAssembler.SetIndexBuffer(_meshIB, Format.R32_UInt, 0);

                var world = _meshWorld;
                var wit = MathF.InverseTranspose(world);
                var wvp = world * viewProj;

                Effects.BasicFX.SetWorld(world);
                Effects.BasicFX.SetWorldInvTranspose(wit);
                Effects.BasicFX.SetWorldViewProj(wvp);
                Effects.BasicFX.SetMaterial(_meshMat);

                var pass = activeTech.GetPassByIndex(p);
                pass.Apply(ImmediateContext);
                ImmediateContext.DrawIndexed(_meshIndexCount, 0, 0);
                ImmediateContext.Rasterizer.State = null;

                if (_pickedTriangle >= 0) {
                    ImmediateContext.OutputMerger.DepthStencilState = RenderStates.LessEqualDSS;
                    ImmediateContext.OutputMerger.DepthStencilReference = 0;

                    Effects.BasicFX.SetMaterial(_pickedTriangleMat);
                    pass.Apply(ImmediateContext);
                    ImmediateContext.DrawIndexed(3, 3*_pickedTriangle, 0);

                    ImmediateContext.OutputMerger.DepthStencilState = null;
                }

            }
            SwapChain.Present(0, PresentFlags.None);
        }
        protected override void OnMouseDown(object sender, MouseEventArgs mouseEventArgs) {
            if (mouseEventArgs.Button == MouseButtons.Left) {
                _lastMousePos = mouseEventArgs.Location;
                Window.Capture = true;
            } else if (mouseEventArgs.Button == MouseButtons.Right) {
                Pick(mouseEventArgs.X, mouseEventArgs.Y);
            }
        }
        protected override void OnMouseUp(object sender, MouseEventArgs e) {
            Window.Capture = false;
        }
        protected override void OnMouseMove(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                var dx = MathF.ToRadians(0.25f * (e.X - _lastMousePos.X));
                var dy = MathF.ToRadians(0.25f * (e.Y - _lastMousePos.Y));

                _cam.Pitch(dy);
                _cam.Yaw(dx);
            }
            _lastMousePos = e.Location;
        }

        private void BuildMeshGeometryBuffers() {
            try {

                var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
                _meshVertices = new List<Basic32>();
                _meshIndices = new List<int>();
                var vcount = 0;
                var tcount = 0;
                using (var reader = new StreamReader("Models\\car.txt")) {


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
                            var position = new Vector3(Convert.ToSingle(vals[0].Trim()), Convert.ToSingle(vals[1].Trim()), Convert.ToSingle(vals[2].Trim()));
                            _meshVertices.Add(
                                new Basic32(
                                    position,
                                    new Vector3(
                                        Convert.ToSingle(vals[3].Trim()),
                                        Convert.ToSingle(vals[4].Trim()),
                                        Convert.ToSingle(vals[5].Trim())),
                                    new Vector2()
                                )
                            );
                            min = Vector3.Minimize(min, position);
                            max = Vector3.Maximize(max, position);
                        }
                    }
                    _meshBox = new BoundingBox(min, max);

                    // skip ahead to the index data
                    do {
                        input = reader.ReadLine();
                    } while (input != null && !input.StartsWith("{"));
                    // Get the indices
                    _meshIndexCount = 3 * tcount;
                    for (var i = 0; i < tcount; i++) {
                        input = reader.ReadLine();
                        if (input == null) {
                            break;
                        }
                        var m = input.Trim().Split(new[] { ' ' });
                        _meshIndices.Add(Convert.ToInt32(m[0].Trim()));
                        _meshIndices.Add(Convert.ToInt32(m[1].Trim()));
                        _meshIndices.Add(Convert.ToInt32(m[2].Trim()));
                    }
                }

                var vbd = new BufferDescription(Basic32.Stride * vcount, ResourceUsage.Immutable,
                    BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
                _meshVB = new Buffer(Device, new DataStream(_meshVertices.ToArray(), false, false), vbd);

                var ibd = new BufferDescription(sizeof(int) * _meshIndexCount, ResourceUsage.Immutable,
                    BindFlags.IndexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
                _meshIB = new Buffer(Device, new DataStream(_meshIndices.ToArray(), false, false), ibd);


            } catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }
        
        private void Pick(int sx, int sy ) {
            
            var ray = _cam.GetPickingRay(new Vector2(sx, sy), new Vector2(ClientWidth, ClientHeight) );

            // transform the picking ray into the object space of the mesh
            var invWorld = Matrix.Invert(_meshWorld);
            ray.Direction = Vector3.TransformNormal(ray.Direction, invWorld);
            ray.Position = Vector3.TransformCoordinate(ray.Position, invWorld);
            ray.Direction.Normalize();
            
            _pickedTriangle = -1;
            float tmin;
            if (!Ray.Intersects(ray, _meshBox, out tmin)) return;

            tmin = float.MaxValue;
            for (var i = 0; i < _meshIndices.Count/3; i++) {
                var v0 = _meshVertices[_meshIndices[i * 3]].Position;
                var v1 = _meshVertices[_meshIndices[i * 3 + 1]].Position;
                var v2 = _meshVertices[_meshIndices[i * 3 + 2]].Position;

                float t;

                float u, v;
                //if (!Ray.Intersects(ray, v0, v1, v2, out t)) continue;
                if (!Ray.Intersects(ray, v0, v1, v2, out t, out u, out v)) continue;

                // determine the actual picked point on the triangle
                var p = v0*(1.0f - u - v) + v1*u + v2*v;

                // find the closest intersection, exclude intersections behind camera
                if (!(t < tmin || t < 0)) continue;
                tmin = t;
                _pickedTriangle = i;
            }
        }
    }

    static class Program {
        static void Main() {
            Configuration.EnableObjectTracking = true;
            var app = new PickingDemo(Process.GetCurrentProcess().Handle);
            if (!app.Init()) {
                return;
            }
            app.Run();
        }
    }
}
