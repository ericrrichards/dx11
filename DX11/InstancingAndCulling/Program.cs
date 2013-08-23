
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
using Core.FX;
using SlimDX;
using SlimDX.DXGI;
using SlimDX.Direct3D11;
using Buffer = SlimDX.Direct3D11.Buffer;
using MapFlags = SlimDX.Direct3D11.MapFlags;

namespace InstancingAndCulling {
    struct InstancedData {
        public Matrix World;
        public Color4 Color;
    }

    public class InstancingAndCullingDemo : D3DApp {
        private Buffer _skullVB;
        private Buffer _skullIB;
        private Buffer _instanceBuffer;

        private BoundingBox _skullBox;
        private Frustum _cameraFrustum;

        private int _visibleObjectCount;
        private List<InstancedData> _instancedData;

        private bool _frustumCullingEnabled;

        private readonly DirectionalLight[] _dirLights;
        private Material _skullMat;

        private Matrix _skullWorld;

        private int _skullIndexCount;

        private Camera _cam;
        private Point _lastMousePos;

        private bool _disposed;
        public InstancingAndCullingDemo(IntPtr hInstance) : base(hInstance) {
            _skullIndexCount = 0;
            _visibleObjectCount = 0;
            _frustumCullingEnabled = true;

            MainWindowCaption = "Instancing and Culling Demo";

            _lastMousePos = new Point();

            _cam = new Camera();
            _cam.Position = new Vector3(0, 2, -15);

            _skullWorld = Matrix.Scaling(0.5f, 0.5f, 0.5f)*Matrix.Translation(0, 1, 0);

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
            _skullMat = new Material {
                Ambient = new Color4(0.4f, 0.4f, 0.4f),
                Diffuse = new Color4(0.8f, 0.8f, 0.8f),
                Specular = new Color4(16.0f, 0.8f, 0.8f, 0.8f)
            };
        }

        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Util.ReleaseCom(ref _skullVB);
                    Util.ReleaseCom(ref _skullIB);
                    Util.ReleaseCom(ref _instanceBuffer);
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

            BuildSkullGeometryBuffers();
            BuildInstancedBuffer();

            return true;
        }
        public override void OnResize() {
            base.OnResize();
            _cam.SetLens(0.25f*MathF.PI, AspectRatio, 1.0f, 1000.0f);
            _cameraFrustum = Frustum.FromProjection(_cam.Proj);
        }
        public override void UpdateScene(float dt) {
            base.UpdateScene(dt);
            if (Util.IsKeyDown(Keys.Up)) {
                _cam.Walk(10.0f*dt);
            }
            if (Util.IsKeyDown(Keys.Down)) {
                _cam.Walk(-10.0f * dt);
            }
            if (Util.IsKeyDown(Keys.Left)) {
                _cam.Strafe(-10.0f * dt);
            }
            if (Util.IsKeyDown(Keys.Right)) {
                _cam.Strafe(10.0f * dt);
            }
            if (Util.IsKeyDown(Keys.D1)) {
                _frustumCullingEnabled = true;
            }
            if (Util.IsKeyDown(Keys.D2)) {
                _frustumCullingEnabled = false;
            }
            _cam.UpdateViewMatrix();
            _visibleObjectCount = 0;
            if (_frustumCullingEnabled) {
                var invView = Matrix.Invert(_cam.View);
                
                var db = ImmediateContext.MapSubresource(_instanceBuffer, MapMode.WriteDiscard, MapFlags.None);
                
                foreach (var instancedData in _instancedData) {
                    var w = instancedData.World;
                    var invWorld = Matrix.Invert(w);
                    var toLocal = invView*invWorld;
                    Vector3 scale;
                    Quaternion rotQuat;
                    Vector3 translation;
                    toLocal.Decompose(out scale, out rotQuat, out translation);

                    var localSpaceFrustum = Frustum.Transform(_cameraFrustum, scale.X, rotQuat, translation);
                    if (localSpaceFrustum.Intersect(_skullBox) != 0) {
                        db.Data.Write(instancedData);
                        _visibleObjectCount++;
                    }
                }

                ImmediateContext.UnmapSubresource(_instanceBuffer, 0);

            } else {
                var db = ImmediateContext.MapSubresource(_instanceBuffer, MapMode.WriteDiscard, MapFlags.None);
                foreach (var instancedData in _instancedData) {
                    db.Data.Write(instancedData);
                    _visibleObjectCount++;
                }

                ImmediateContext.UnmapSubresource(_instanceBuffer, 0);
            }
            MainWindowCaption = String.Format("Instancing and Culling Demo    {0} objects visible out of {1}", _visibleObjectCount, _instancedData.Count);
        }

        public override void DrawScene() {
            base.DrawScene();
            ImmediateContext.ClearRenderTargetView(RenderTargetView, Color.Silver);
            ImmediateContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth|DepthStencilClearFlags.Stencil,1.0f, 0 );

            ImmediateContext.InputAssembler.InputLayout = InputLayouts.InstancedBasic32;
            ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            var stride = new[] {Basic32.Stride, Marshal.SizeOf(typeof (InstanceData))};
            var offset = new[] {0, 0};
            
            var view = _cam.View;
            var proj = _cam.Proj;
            var viewProj = _cam.ViewProj;

            Effects.InstancedBasicFX.SetDirLights(_dirLights);
            Effects.InstancedBasicFX.SetEyePosW(_cam.Position);
            var activeTech = Effects.InstancedBasicFX.Light3Tech;

            

            for (int p = 0; p < activeTech.Description.PassCount; p++) {
                ImmediateContext.InputAssembler.SetVertexBuffers(
                    0, 
                    new VertexBufferBinding(_skullVB, stride[0], offset[0]), 
                    new VertexBufferBinding(_instanceBuffer, stride[1], offset[1])
                );
                ImmediateContext.InputAssembler.SetIndexBuffer(_skullIB, Format.R32_UInt, 0);

                var world = _skullWorld;
                var wit = MathF.InverseTranspose(world);

                Effects.InstancedBasicFX.SetWorld(world);
                Effects.InstancedBasicFX.SetWorldInvTranspose(wit);
                Effects.InstancedBasicFX.SetViewProj(viewProj);
                Effects.InstancedBasicFX.SetMaterial(_skullMat);

                activeTech.GetPassByIndex(p).Apply(ImmediateContext);
                ImmediateContext.DrawIndexedInstanced(_skullIndexCount, _visibleObjectCount, 0, 0, 0);
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

                _cam.Pitch(dy);
                _cam.Yaw(dx);
            }
            _lastMousePos = e.Location;
        }

        private void BuildSkullGeometryBuffers() {
            try {
                var vertices = new List<Basic32>();
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
                                new Basic32(
                                    new Vector3(
                                        Convert.ToSingle(vals[0].Trim()),
                                        Convert.ToSingle(vals[1].Trim()),
                                        Convert.ToSingle(vals[2].Trim())),
                                    new Vector3(
                                        Convert.ToSingle(vals[3].Trim()),
                                        Convert.ToSingle(vals[4].Trim()),
                                        Convert.ToSingle(vals[5].Trim())),
                                    new Vector2()
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

                var vbd = new BufferDescription(Basic32.Stride * vcount, ResourceUsage.Immutable,
                    BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
                _skullVB = new Buffer(Device, new DataStream(vertices.ToArray(), false, false), vbd);

                var ibd = new BufferDescription(sizeof(int) * _skullIndexCount, ResourceUsage.Immutable,
                    BindFlags.IndexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
                _skullIB = new Buffer(Device, new DataStream(indices.ToArray(), false, false), ibd);


            } catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }
        private void BuildInstancedBuffer() {
            const int n = 5;
            var width = 200.0f;
            var height = 200.0f;
            var depth = 200.0f;

            var x = -0.5f*width;
            var y = -0.5f*height;
            var z = -0.5f*depth;
            var dx = width/(n - 1);
            var dy = height/(n - 1);
            var dz = depth/(n - 1);

            for (int k = 0; k< n; k++) {
                for (int i = 0; i < n; i++) {
                    for (int j = 0; j < n; j++) {
                        _instancedData.Add(new InstancedData() {
                            World = Matrix.Translation(x+j*dx, y+i*dy, z+k*dz),
                            Color = new Color4(MathF.Rand(0,1), MathF.Rand(0,1), MathF.Rand(0,1))
                        });
                    }
                }
            }
            var vbd = new BufferDescription(Marshal.SizeOf(typeof (InstanceData)), ResourceUsage.Dynamic, BindFlags.VertexBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
            _instanceBuffer = new Buffer(Device, new DataStream(_instancedData.ToArray(), false, true), vbd);
        }
    }

    class Program {
        static void Main(string[] args) {
            SlimDX.Configuration.EnableObjectTracking = true;
            var app = new InstancingAndCullingDemo(Process.GetCurrentProcess().Handle);
            if (!app.Init()) {
                return;
            }
            app.Run();
        }
    }
}
