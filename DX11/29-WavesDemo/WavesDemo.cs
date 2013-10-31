using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Core;
using Core.Camera;
using Core.FX;
using Core.Model;
using Core.Vertex;
using SlimDX;
using SlimDX.Direct3D11;
using SlimDX.DXGI;
using Buffer = SlimDX.Direct3D11.Buffer;
using Waves = Core.Model.Waves;

namespace _29_WavesDemo {
    class WavesDemo : D3DApp {
        private Sky _sky;
        private Buffer _skullVB;
        private Buffer _skullIB;

        private readonly DirectionalLight[] _dirLights;
        private TextureManager _texMgr;

        private Waves _waves;
        

        private readonly Material _skullMat;
        private readonly Matrix _skullWorld;
        private int _skullIndexCount;

        private readonly FpsCamera _camera;
        private Point _lastMousePos;

        private BasicModel _boxModel;
        private BasicModel _sphereModel;
        private BasicModel _cylinderModel;
        

        private BasicModelInstance _box;
        private readonly BasicModelInstance[] _spheres = new BasicModelInstance[10];
        private readonly BasicModelInstance[] _cylinders = new BasicModelInstance[10];
        
        private bool _disposed;

        protected WavesDemo(IntPtr hInstance) : base(hInstance) {
            

            MainWindowCaption = "Waves Demo";

            _lastMousePos = new Point();

            _camera = new FpsCamera {Position = new Vector3(0, 2, -15)};

            _skullWorld = Matrix.Scaling(0.5f, 0.5f, 0.5f) * Matrix.Translation(0, 1.0f, 0);

            _skullMat = new Material {
                Ambient = new Color4(0.2f, 0.2f, 0.2f),
                Diffuse = new Color4(0.2f, 0.2f, 0.2f),
                Specular = new Color4(16.0f, 0.8f, 0.8f, 0.8f),
                Reflect = new Color4(0.5f, 0.5f, 0.5f)
            };
            _dirLights = new[] {
                new DirectionalLight {
                    Ambient = new Color4(0.2f, 0.2f, 0.2f),
                    Diffuse = new Color4(0.5f, 0.5f, 0.5f),
                    Specular = new Color4(0.5f, 0.5f, 0.5f),
                    Direction = new Vector3(0f, -0.31622f, -0.9486f)
                },
                new DirectionalLight {
                    Ambient = new Color4(0,0,0),
                    Diffuse = new Color4( 0.4f, 0.4f, 0.4f),
                    Specular = new Color4( 0.45f, 0.45f, 0.45f),
                    Direction = new Vector3(0.57735f, 0.57735f, 0.57735f)
                },
                new DirectionalLight {
                    Ambient = new Color4(0,0,0),
                    Diffuse = new Color4(0.2f, 0.2f, 0.2f),
                    Specular = new Color4(0f,0f,0f),
                    Direction = new Vector3(0, -0.707f, 0.707f)
                }
            };

        }
        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Util.ReleaseCom(ref _sky);

                    Util.ReleaseCom(ref _skullVB);
                    Util.ReleaseCom(ref _skullIB);

                    Util.ReleaseCom(ref _texMgr);

                    Util.ReleaseCom(ref _boxModel);
                    Util.ReleaseCom(ref _sphereModel);
                    Util.ReleaseCom(ref _cylinderModel);
                    Util.ReleaseCom(ref _waves);


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

            _sky = new Sky(Device, "Textures/snowcube1024.dds", 5000.0f);
            
            _texMgr = new TextureManager();
            _texMgr.Init(Device);

            _waves = new Waves();
            _waves.Init(Device, _texMgr, 40, 60);


            BuildShapeGeometryBuffers();
            BuildSkullGeometryBuffers();


            return true;
        }
        public override void OnResize() {
            base.OnResize();
            _camera.SetLens(0.25f * MathF.PI, AspectRatio, 1.0f, 1000.0f);
        }
        public override void UpdateScene(float dt) {
            base.UpdateScene(dt);
            if (Util.IsKeyDown(Keys.Up)) {
                _camera.Walk(10.0f * dt);
            }
            if (Util.IsKeyDown(Keys.Down)) {
                _camera.Walk(-10.0f * dt);
            }
            if (Util.IsKeyDown(Keys.Left)) {
                _camera.Strafe(-10.0f * dt);
            }
            if (Util.IsKeyDown(Keys.Right)) {
                _camera.Strafe(10.0f * dt);
            }
            if (Util.IsKeyDown(Keys.PageUp)) {
                _camera.Zoom(-dt);
            }
            if (Util.IsKeyDown(Keys.PageDown)) {
                _camera.Zoom(+dt);
            }

            _waves.Update(dt);
            _camera.UpdateViewMatrix();

        }

        public override void DrawScene() {
            ImmediateContext.ClearRenderTargetView(RenderTargetView, Color.Silver);
            ImmediateContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth|DepthStencilClearFlags.Stencil, 1.0f, 0 );

            var viewProj = _camera.ViewProj;

            Effects.BasicFX.SetDirLights(_dirLights);
            Effects.BasicFX.SetEyePosW(_camera.Position);
            Effects.BasicFX.SetCubeMap(_sky.CubeMapSRV);

            Effects.NormalMapFX.SetDirLights(_dirLights);
            Effects.NormalMapFX.SetEyePosW(_camera.Position);
            Effects.NormalMapFX.SetCubeMap(_sky.CubeMapSRV);

            Effects.DisplacementMapFX.SetDirLights(_dirLights);
            Effects.DisplacementMapFX.SetEyePosW(_camera.Position);
            Effects.DisplacementMapFX.SetCubeMap(_sky.CubeMapSRV);

            Effects.DisplacementMapFX.SetHeightScale(0.07f);
	        Effects.DisplacementMapFX.SetMaxTessDistance(1.0f);
	        Effects.DisplacementMapFX.SetMinTessDistance(25.0f);
	        Effects.DisplacementMapFX.SetMinTessFactor(1.0f);
	        Effects.DisplacementMapFX.SetMaxTessFactor(5.0f);

            Effects.WavesFX.SetDirLights(_dirLights);
            Effects.WavesFX.SetEyePosW(_camera.Position);
            Effects.WavesFX.SetCubeMap(_sky.CubeMapSRV);

            Effects.WavesFX.SetHeightScale0(0.4f);
            Effects.WavesFX.SetHeightScale1(1.2f);
            Effects.WavesFX.SetMaxTessDistance(4.0f);
            Effects.WavesFX.SetMinTessDistance(30.0f);
            Effects.WavesFX.SetMinTessFactor(2.0f);
            Effects.WavesFX.SetMaxTessFactor(6.0f);

            var activeSkullTech = Effects.BasicFX.Light3ReflectTech;
            var waveTech = Effects.WavesFX.Light3ReflectTech;
            var activeTech = Effects.DisplacementMapFX.Light3TexTech;
            var activeSphereTech = Effects.BasicFX.Light3ReflectTech;

            ImmediateContext.InputAssembler.InputLayout = InputLayouts.PosNormalTexTan;
            ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.PatchListWith3ControlPoints;

            if (Util.IsKeyDown(Keys.W)) {
                ImmediateContext.Rasterizer.State = RenderStates.WireframeRS;
            }

            _waves.Draw(ImmediateContext, waveTech, viewProj);
            
            for (var p = 0; p < activeTech.Description.PassCount; p++) {
                var pass = activeTech.GetPassByIndex(p);
                
                
                _box.DrawDisplaced(ImmediateContext, pass, viewProj);

                // draw columns
                foreach (var cylinder in _cylinders) {
                    
                    cylinder.DrawDisplaced(ImmediateContext, pass, viewProj);
                }

            }
            ImmediateContext.HullShader.Set(null);
            ImmediateContext.DomainShader.Set(null);
            ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            for (var p = 0; p < activeSphereTech.Description.PassCount; p++) {
                var pass = activeSphereTech.GetPassByIndex(p);

                foreach (var sphere in _spheres) {
                    sphere.DrawBasic(ImmediateContext, pass, viewProj);
                }

            }
            var stride = Basic32.Stride;
            const int offset = 0;

            ImmediateContext.Rasterizer.State = null;

            ImmediateContext.InputAssembler.InputLayout = InputLayouts.Basic32;
            ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_skullVB, stride, offset));
            ImmediateContext.InputAssembler.SetIndexBuffer(_skullIB, Format.R32_UInt, 0);

            for (int p = 0; p < activeSkullTech.Description.PassCount; p++) {
                var world = _skullWorld;
                var wit = MathF.InverseTranspose(world);
                var wvp = world * viewProj;

                Effects.BasicFX.SetWorld(world);
                Effects.BasicFX.SetWorldInvTranspose(wit);
                Effects.BasicFX.SetWorldViewProj(wvp);
                Effects.BasicFX.SetMaterial(_skullMat);

                activeSkullTech.GetPassByIndex(p).Apply(ImmediateContext);
                ImmediateContext.DrawIndexed(_skullIndexCount, 0, 0);

            }

            _sky.Draw(ImmediateContext, _camera);

            ImmediateContext.Rasterizer.State = null;
            ImmediateContext.OutputMerger.DepthStencilState = null;
            ImmediateContext.OutputMerger.DepthStencilReference = 0;

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

                _camera.Pitch(dy);
                _camera.Yaw(dx);

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
        private void BuildShapeGeometryBuffers() {
            
            _boxModel = BasicModel.CreateBox(Device, 1, 1, 1);
            _boxModel.Materials[0] = new Material {
                Ambient = new Color4(1f, 1f, 1f),
                Diffuse = new Color4(1f, 1f, 1f),
                Specular = new Color4(16.0f, 0.8f, 0.8f, 0.8f),
                Reflect = Color.Black
            };
            _boxModel.DiffuseMapSRV[0] = _texMgr.CreateTexture("Textures/bricks.dds");
            _boxModel.NormalMapSRV[0] = _texMgr.CreateTexture("Textures/bricks_nmap.dds");

            

            _sphereModel = BasicModel.CreateSphere(Device, 0.5f, 20, 20);
            _sphereModel.Materials[0] = new Material {
                Ambient = new Color4(0.2f, 0.3f, 0.4f),
                Diffuse = new Color4(0.2f, 0.3f, 0.4f),
                Specular = new Color4(16.0f, 0.9f, 0.9f, 0.9f),
                Reflect = new Color4(0.4f, 0.4f, 0.4f)
            };
            _cylinderModel = BasicModel.CreateCylinder(Device, 0.5f, 0.3f, 3.0f, 15, 15);
            _cylinderModel.Materials[0] = new Material {
                Ambient = new Color4(1f, 1f, 1f),
                Diffuse = new Color4(1f, 1f, 1f),
                Specular = new Color4(32.0f, 1f, 1f, 1f),
                Reflect = Color.Black
            };
            _cylinderModel.DiffuseMapSRV[0] = _texMgr.CreateTexture("Textures/bricks.dds");
            _cylinderModel.NormalMapSRV[0] = _texMgr.CreateTexture("Textures/bricks_nmap.dds");

            for (var i = 0; i < 5; i++) {
                _cylinders[i * 2] = new BasicModelInstance {
                    Model = _cylinderModel,
                    World = Matrix.Translation(-5.0f, 1.5f, -10.0f + i * 5.0f),
                    TexTransform = Matrix.Scaling(1, 2, 1)
                };
                _cylinders[i * 2 + 1] = new BasicModelInstance {
                    Model = _cylinderModel,
                    World = Matrix.Translation(5.0f, 1.5f, -10.0f + i * 5.0f),
                    TexTransform = Matrix.Scaling(1, 2, 1)
                };

                _spheres[i * 2] = new BasicModelInstance {
                    Model = _sphereModel,
                    World = Matrix.Translation(-5.0f, 3.45f, -10.0f + i * 5.0f)
                };
                _spheres[i * 2 + 1] = new BasicModelInstance {
                    Model = _sphereModel,
                    World = Matrix.Translation(5.0f, 3.45f, -10.0f + i * 5.0f)
                };
            }

            _box = new BasicModelInstance {
                Model = _boxModel,
                TexTransform = Matrix.Scaling(2, 1, 1),
                World = Matrix.Scaling(3.0f, 1.0f, 3.0f) * Matrix.Translation(0, 0.5f, 0)
            };
            


        }

        static void Main() {
            Configuration.EnableObjectTracking = true;
            var app = new WavesDemo(Process.GetCurrentProcess().Handle);
            if (!app.Init()) {
                return;
            }
            try {
                app.Run();
            } catch (Exception ex) {
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
            }
        }
    }
}
