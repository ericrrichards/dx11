using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
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

namespace _28_SsaoDemo {
    class SsaoDemo: D3DApp {

        private Sky _sky;

        private Buffer _skullVB;
        private Buffer _skullIB;
        private Buffer _screenQuadVB;
        private Buffer _screenQuadIB;

        private TextureManager _texMgr;

        private BoundingSphere _sceneBounds;

        private const int SMapSize = 2048;
        private ShadowMap _sMap;
        private Matrix _lightView;
        private Matrix _lightProj;
        private Matrix _shadowTransform;

        private SSAO _ssao;

        private float _lightRotationAngle;
        private readonly Vector3[] _originalLightDirs;
        private readonly DirectionalLight[] _dirLights;

        private readonly Material _skullMat;
        private readonly Matrix _skullWorld;
        private int _skullIndexCount;

        private readonly FpsCamera _camera;
        private Point _lastMousePos;

        private BasicModel _boxModel;
        private BasicModel _gridModel;
        private BasicModel _sphereModel;
        private BasicModel _cylinderModel;

        private BasicModelInstance _grid;
        private BasicModelInstance _box;
        private readonly BasicModelInstance[] _spheres = new BasicModelInstance[10];
        private readonly BasicModelInstance[] _cylinders = new BasicModelInstance[10];

        private bool _disposed;

        protected SsaoDemo(IntPtr hInstance) : base(hInstance) {
            _lightRotationAngle = 0.0f;

            MainWindowCaption = "SSAO Demo";

            _lastMousePos = new Point();

            _camera = new FpsCamera {Position = new Vector3(0, 2, -15)};

            _sceneBounds = new BoundingSphere(new Vector3(), MathF.Sqrt(10 * 10 + 15 * 15));

            _skullWorld = Matrix.Scaling(0.5f, 0.5f, 0.5f) * Matrix.Translation(0, 1.0f, 0);


            _dirLights = new[] {
                new DirectionalLight {
                    Ambient = new Color4(1f, 1f, 1f),
                    Diffuse = new Color4(0.5f, 0.5f, 0.4f),
                    Specular = new Color4(0.8f, 0.8f, 0.7f),
                    Direction = new Vector3(-0.57735f, -0.57735f, 0.57735f)
                },
                new DirectionalLight {
                    Ambient = new Color4(0,0,0),
                    Diffuse = new Color4( 0.4f, 0.4f, 0.4f),
                    Specular = new Color4( 0.2f, 0.2f, 0.2f),
                    Direction = new Vector3(0.707f, -0.707f, 0)
                },
                new DirectionalLight {
                    Ambient = new Color4(0,0,0),
                    Diffuse = new Color4(0.2f, 0.2f, 0.2f),
                    Specular = new Color4(0.2f,0.2f,0.2f),
                    Direction = new Vector3(0, 0, -1)
                }
            };

            _originalLightDirs = _dirLights.Select(l => l.Direction).ToArray();


            _skullMat = new Material {
                Ambient = new Color4(0.5f, 0.5f, 0.5f),
                Diffuse = new Color4(0.2f, 0.2f, 0.2f),
                Specular = new Color4(16.0f, 0.5f, 0.5f, 0.5f),
                Reflect = new Color4(0.3f, 0.3f, 0.3f)
            };
        }
        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Util.ReleaseCom(ref _sky);
                    Util.ReleaseCom(ref _sMap);
                    Util.ReleaseCom(ref _ssao);

                    Util.ReleaseCom(ref _skullVB);
                    Util.ReleaseCom(ref _skullIB);

                    Util.ReleaseCom(ref _screenQuadVB);
                    Util.ReleaseCom(ref _screenQuadIB);


                    Util.ReleaseCom(ref _texMgr);

                    Util.ReleaseCom(ref _gridModel);
                    Util.ReleaseCom(ref _boxModel);
                    Util.ReleaseCom(ref _sphereModel);
                    Util.ReleaseCom(ref _cylinderModel);



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

            _sky = new Sky(Device, "Textures/desertcube1024.dds", 5000.0f);
            _sMap = new ShadowMap(Device, SMapSize, SMapSize);

            _camera.SetLens(0.25f *MathF.PI, AspectRatio, 1.0f, 1000.0f);
            _ssao = new SSAO(Device, ImmediateContext, ClientWidth, ClientHeight, _camera.FovY, _camera.FarZ);


            BuildShapeGeometryBuffers();
            BuildSkullGeometryBuffers();
            BuildScreenQuadGeometryBuffers();


            return true;
        }
        private void BuildShapeGeometryBuffers() {
            _texMgr = new TextureManager();
            _texMgr.Init(Device);

            _boxModel = BasicModel.CreateBox(Device, 1, 1, 1);
            _boxModel.Materials[0] = new Material {
                Ambient = new Color4(0.8f, 0.8f, 0.8f),
                Diffuse = new Color4(0.4f, 0.4f, 0.4f),
                Specular = new Color4(16.0f, 0.8f, 0.8f, 0.8f),
                Reflect = Color.Black
            };
            _boxModel.DiffuseMapSRV[0] = _texMgr.CreateTexture("Textures/bricks.dds");
            _boxModel.NormalMapSRV[0] = _texMgr.CreateTexture("Textures/bricks_nmap.dds");

            _gridModel = BasicModel.CreateGrid(Device, 20, 30, 50, 40);
            _gridModel.Materials[0] = new Material {
                Ambient = new Color4(0.7f, 0.7f, 0.7f),
                Diffuse = new Color4(0.6f, 0.6f, 0.6f),
                Specular = new Color4(16.0f, 0.4f, 0.4f, 0.4f),
                Reflect = Color.Black
            };
            _gridModel.DiffuseMapSRV[0] = _texMgr.CreateTexture("Textures/floor.dds");
            _gridModel.NormalMapSRV[0] = _texMgr.CreateTexture("Textures/floor_nmap.dds");


            _sphereModel = BasicModel.CreateSphere(Device, 0.5f, 20, 20);
            _sphereModel.Materials[0] = new Material {
                Ambient = new Color4(0.3f, 0.4f, 0.5f),
                Diffuse = new Color4(0.2f, 0.3f, 0.4f),
                Specular = new Color4(16.0f, 0.9f, 0.9f, 0.9f),
                Reflect = new Color4(0.3f, 0.3f, 0.3f)
            };
            _cylinderModel = BasicModel.CreateCylinder(Device, 0.5f, 0.5f, 3.0f, 15, 15);
            _cylinderModel.Materials[0] = new Material {
                Ambient = new Color4(0.8f, 0.8f, 0.8f),
                Diffuse = new Color4(0.4f, 0.4f, 0.4f),
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
                    World = Matrix.Translation(-5.0f, 3.5f, -10.0f + i * 5.0f)
                };
                _spheres[i * 2 + 1] = new BasicModelInstance {
                    Model = _sphereModel,
                    World = Matrix.Translation(5.0f, 3.5f, -10.0f + i * 5.0f)
                };
            }

            _grid = new BasicModelInstance {
                Model = _gridModel,
                TexTransform = Matrix.Scaling(8, 10, 1),
                World = Matrix.Identity
            };

            _box = new BasicModelInstance {
                Model = _boxModel,
                TexTransform = Matrix.Scaling(2, 1, 1),
                World = Matrix.Scaling(3.0f, 1.0f, 3.0f) * Matrix.Translation(0, 0.5f, 0)
            };


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
        private void BuildScreenQuadGeometryBuffers() {
            var quad = GeometryGenerator.CreateFullScreenQuad();

            var verts = quad.Vertices.Select(v => new Basic32(v.Position, v.Normal, v.TexC)).ToList();
            var vbd = new BufferDescription(Basic32.Stride * verts.Count, ResourceUsage.Immutable, BindFlags.VertexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _screenQuadVB = new Buffer(Device, new DataStream(verts.ToArray(), false, false), vbd);

            var ibd = new BufferDescription(sizeof(int) * quad.Indices.Count, ResourceUsage.Immutable, BindFlags.IndexBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _screenQuadIB = new Buffer(Device, new DataStream(quad.Indices.ToArray(), false, false), ibd);
        }

        public override void OnResize() {
            base.OnResize();
            _camera.SetLens(0.25f * MathF.PI, AspectRatio, 1.0f, 1000.0f);

            if (_ssao != null) {
                _ssao.OnSize(ClientWidth, ClientHeight, _camera.FovY, _camera.FarZ);
            }
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

            _lightRotationAngle += 0.1f * dt;

            var r = Matrix.RotationY(_lightRotationAngle);
            for (int i = 0; i < 3; i++) {
                var lightDir = _originalLightDirs[i];
                lightDir = Vector3.TransformNormal(lightDir, r);
                _dirLights[i].Direction = lightDir;
            }

            BuildShadowTransform();
            _camera.UpdateViewMatrix();

        }
        private void BuildShadowTransform() {
            var lightDir = _dirLights[0].Direction;
            var lightPos = -2.0f * _sceneBounds.Radius * lightDir;
            var targetPos = _sceneBounds.Center;
            var up = new Vector3(0, 1, 0);

            var v = Matrix.LookAtLH(lightPos, targetPos, up);

            var sphereCenterLS = Vector3.TransformCoordinate(targetPos, v);

            var l = sphereCenterLS.X - _sceneBounds.Radius;
            var b = sphereCenterLS.Y - _sceneBounds.Radius;
            var n = sphereCenterLS.Z - _sceneBounds.Radius;
            var r = sphereCenterLS.X + _sceneBounds.Radius;
            var t = sphereCenterLS.Y + _sceneBounds.Radius;
            var f = sphereCenterLS.Z + _sceneBounds.Radius;


            //var p = Matrix.OrthoLH(r - l, t - b+5, n, f);
            var p = Matrix.OrthoOffCenterLH(l, r, b, t, n, f);
            var T = new Matrix {
                M11 = 0.5f,
                M22 = -0.5f,
                M33 = 1.0f,
                M41 = 0.5f,
                M42 = 0.5f,
                M44 = 1.0f
            };



            var s = v * p * T;
            _lightView = v;
            _lightProj = p;
            _shadowTransform = s;
        }
        public override void DrawScene() {

            _sMap.BindDsvAndSetNullRenderTarget(ImmediateContext);

            DrawSceneToShadowMap();

            ImmediateContext.Rasterizer.State = null;

            ImmediateContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);
            ImmediateContext.Rasterizer.SetViewports(Viewport);
            _ssao.SetNormalDepthRenderTarget(DepthStencilView);

            DrawSceneToSsaoNormalDepthMap();

            _ssao.ComputeSsao(_camera);
            _ssao.BlurAmbientMap(4);


            ImmediateContext.OutputMerger.SetTargets(DepthStencilView, RenderTargetView);
            ImmediateContext.Rasterizer.SetViewports(Viewport);

            ImmediateContext.ClearRenderTargetView(RenderTargetView, Color.Silver);

            ImmediateContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);
            //ImmediateContext.OutputMerger.DepthStencilState = RenderStates.EqualsDSS;
            //ImmediateContext.OutputMerger.DepthStencilReference = 0;


            var viewProj = _camera.ViewProj;

            Effects.BasicFX.SetDirLights(_dirLights);
            Effects.BasicFX.SetEyePosW(_camera.Position);
            Effects.BasicFX.SetCubeMap(_sky.CubeMapSRV);
            Effects.BasicFX.SetShadowMap(_sMap.DepthMapSRV);
            Effects.BasicFX.SetSsaoMap(_ssao.AmbientSRV);

            Effects.NormalMapFX.SetDirLights(_dirLights);
            Effects.NormalMapFX.SetEyePosW(_camera.Position);
            Effects.NormalMapFX.SetCubeMap(_sky.CubeMapSRV);
            Effects.NormalMapFX.SetShadowMap(_sMap.DepthMapSRV);
            Effects.NormalMapFX.SetSsaoMap(_ssao.AmbientSRV);

            var activeTech = Effects.NormalMapFX.Light3TexTech;
            var activeSphereTech = Effects.BasicFX.Light3ReflectTech;
            var activeSkullTech = Effects.BasicFX.Light3ReflectTech;

            ImmediateContext.InputAssembler.InputLayout = InputLayouts.PosNormalTexTan;
            ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            var toTexSpace = Matrix.Identity;
            toTexSpace.M11 = 0.5f;
            toTexSpace.M22 = -0.5f;
            toTexSpace.M41 = 0.5f;
            toTexSpace.M42 = 0.5f;

            

            if (Util.IsKeyDown(Keys.W)) {
                ImmediateContext.Rasterizer.State = RenderStates.WireframeRS;
            }
            for (var p = 0; p < activeTech.Description.PassCount; p++) {
                // draw grid
                var pass = activeTech.GetPassByIndex(p);
                _grid.ShadowTransform = _shadowTransform;
                _grid.ToTexSpace = toTexSpace;
                _grid.Draw(ImmediateContext, pass, viewProj);
                // draw box
                _box.ShadowTransform = _shadowTransform;
                _box.ToTexSpace = toTexSpace;
                _box.Draw(ImmediateContext, pass, viewProj);

                // draw columns
                foreach (var cylinder in _cylinders) {
                    cylinder.ShadowTransform = _shadowTransform;
                    cylinder.ToTexSpace = toTexSpace;
                    cylinder.Draw(ImmediateContext, pass, viewProj);
                }

            }
            ImmediateContext.HullShader.Set(null);
            ImmediateContext.DomainShader.Set(null);
            ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            for (var p = 0; p < activeSphereTech.Description.PassCount; p++) {
                var pass = activeSphereTech.GetPassByIndex(p);

                foreach (var sphere in _spheres) {
                    sphere.ShadowTransform = _shadowTransform;
                    sphere.ToTexSpace = toTexSpace;
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
                Effects.BasicFX.SetWorldViewProjTex(wvp*toTexSpace);
                Effects.BasicFX.SetShadowTransform(world * _shadowTransform);
                Effects.BasicFX.SetMaterial(_skullMat);

                activeSkullTech.GetPassByIndex(p).Apply(ImmediateContext);
                ImmediateContext.DrawIndexed(_skullIndexCount, 0, 0);

            }
            ImmediateContext.OutputMerger.DepthStencilState = null;
            ImmediateContext.OutputMerger.DepthStencilReference = 0;

            DrawScreenQuad(_ssao.AmbientSRV);

            _sky.Draw(ImmediateContext, _camera);

            ImmediateContext.Rasterizer.State = null;
            ImmediateContext.OutputMerger.DepthStencilState = null;
            ImmediateContext.OutputMerger.DepthStencilReference = 0;

            var srvs = new List<ShaderResourceView>();
            for (int i = 0; i < 16; i++) {
                srvs.Add(null);
            }

            ImmediateContext.PixelShader.SetShaderResources(srvs.ToArray(), 0, 16);

            SwapChain.Present(0, PresentFlags.None);
        }

        private void DrawSceneToSsaoNormalDepthMap() {
            var view = _camera.View;
            var proj = _camera.Proj;

            var tech = Effects.SsaoNormalDepthFX.NormalDepthTech;

            ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            ImmediateContext.InputAssembler.InputLayout = InputLayouts.PosNormalTexTan;
            for (int p = 0; p < tech.Description.PassCount; p++) {
                var pass = tech.GetPassByIndex(p);
                _grid.DrawSsaoDepth(ImmediateContext, pass, view, proj);
                _box.DrawSsaoDepth(ImmediateContext, pass, view, proj);
                foreach (var cylinder in _cylinders) {
                    cylinder.DrawSsaoDepth(ImmediateContext, pass, view, proj);
                }
                foreach (var sphere in _spheres) {
                    sphere.DrawSsaoDepth(ImmediateContext, pass, view, proj);
                }

            }
            ImmediateContext.Rasterizer.State = null;
            var stride = Basic32.Stride;
            const int offset = 0;
            ImmediateContext.InputAssembler.InputLayout = InputLayouts.Basic32;
            ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_skullVB, stride, offset));
            ImmediateContext.InputAssembler.SetIndexBuffer(_skullIB, Format.R32_UInt, 0);

            for (int p = 0; p < tech.Description.PassCount; p++) {
                var world = _skullWorld;
                var wit = MathF.InverseTranspose(world);
                var wv = world*view;
                var witv = wit*view;
                var wvp = world*view*proj;

                Effects.SsaoNormalDepthFX.SetWorldView(wv);
                Effects.SsaoNormalDepthFX.SetWorldInvTransposeView(witv);
                Effects.SsaoNormalDepthFX.SetWorldViewProj(wvp);
                Effects.SsaoNormalDepthFX.SetTexTransform(Matrix.Identity);

                tech.GetPassByIndex(p).Apply(ImmediateContext);
                ImmediateContext.DrawIndexed(_skullIndexCount, 0, 0);
            }

        }

        private void DrawSceneToShadowMap() {
            try {
                var view = _lightView;
                var proj = _lightProj;
                var viewProj = view * proj;

                Effects.BuildShadowMapFX.SetEyePosW(_camera.Position);
                Effects.BuildShadowMapFX.SetViewProj(viewProj);

                ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
                var smapTech = Effects.BuildShadowMapFX.BuildShadowMapTech;

                const int offset = 0;

                ImmediateContext.InputAssembler.InputLayout = InputLayouts.PosNormalTexTan;

                if (Util.IsKeyDown(Keys.W)) {
                    ImmediateContext.Rasterizer.State = RenderStates.WireframeRS;
                }

                for (int p = 0; p < smapTech.Description.PassCount; p++) {
                    var pass = smapTech.GetPassByIndex(p);
                    _grid.DrawShadow(ImmediateContext, pass, viewProj);

                    _box.DrawShadow(ImmediateContext, pass, viewProj);

                    foreach (var cylinder in _cylinders) {
                        cylinder.DrawShadow(ImmediateContext, pass, viewProj);
                    }
                }

                ImmediateContext.HullShader.Set(null);
                ImmediateContext.DomainShader.Set(null);
                ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

                for (var p = 0; p < smapTech.Description.PassCount; p++) {
                    var pass = smapTech.GetPassByIndex(p);
                    foreach (var sphere in _spheres) {
                        sphere.DrawShadow(ImmediateContext, pass, viewProj);
                    }
                }
                int stride = Basic32.Stride;
                ImmediateContext.Rasterizer.State = null;

                ImmediateContext.InputAssembler.InputLayout = InputLayouts.Basic32;
                ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_skullVB, stride, offset));
                ImmediateContext.InputAssembler.SetIndexBuffer(_skullIB, Format.R32_UInt, 0);

                for (var p = 0; p < smapTech.Description.PassCount; p++) {
                    var world = _skullWorld;
                    var wit = MathF.InverseTranspose(world);
                    var wvp = world * viewProj;

                    Effects.BuildShadowMapFX.SetWorld(world);
                    Effects.BuildShadowMapFX.SetWorldInvTranspose(wit);
                    Effects.BuildShadowMapFX.SetWorldViewProj(wvp);
                    Effects.BuildShadowMapFX.SetTexTransform(Matrix.Scaling(1, 2, 1));
                    smapTech.GetPassByIndex(p).Apply(ImmediateContext);
                    ImmediateContext.DrawIndexed(_skullIndexCount, 0, 0);
                }

            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }
        private void DrawScreenQuad(ShaderResourceView srv) {
            var stride = Basic32.Stride;
            const int offset = 0;

            ImmediateContext.InputAssembler.InputLayout = InputLayouts.Basic32;
            ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_screenQuadVB, stride, offset));
            ImmediateContext.InputAssembler.SetIndexBuffer(_screenQuadIB, Format.R32_UInt, 0);

            var world = new Matrix {
                M11 = 0.5f,
                M22 = 0.5f,
                M33 = 1.0f,
                M41 = 0.5f,
                M42 = -0.5f,
                M44 = 1.0f
            };
            var tech = Effects.DebugTexFX.ViewRedTech;
            for (int p = 0; p < tech.Description.PassCount; p++) {
                Effects.DebugTexFX.SetWorldViewProj(world);
                Effects.DebugTexFX.SetTexture(srv);
                tech.GetPassByIndex(p).Apply(ImmediateContext);
                ImmediateContext.DrawIndexed(6, 0, 0);
            }
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

        static void Main() {
            Configuration.EnableObjectTracking = true;
            var app = new SsaoDemo(Process.GetCurrentProcess().Handle);
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
