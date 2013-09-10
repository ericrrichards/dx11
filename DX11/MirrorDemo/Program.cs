using System;

namespace MirrorDemo {
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Windows.Forms;

    using Core.FX;
    using Core.Vertex;

    using SlimDX.Direct3D11;
    using SlimDX.DXGI;

    using Buffer = SlimDX.Direct3D11.Buffer;

    using Core;

    using SlimDX;

    public class MirrorDemo : D3DApp {

        private Buffer _roomVB;
        private Buffer _skullVB;
        private Buffer _skullIB;

        private ShaderResourceView _floorDiffuseMapSRV;
        private ShaderResourceView _wallDiffuseMapSRV;
        private ShaderResourceView _mirrorDiffuseMapSRV;

        private readonly DirectionalLight[] _dirLights;
        private readonly Material _roomMat;
        private readonly Material _skullMat;
        private readonly Material _mirrorMat;
        private readonly Material _shadowMat;

        private readonly Matrix _roomWorld;
        private Matrix _skullWorld;

        private int _skullIndexCount;
        private Vector3 _skullTranslation;

        private Matrix _view;
        private Matrix _proj;

        private RenderOptions _renderOptions;

        private Vector3 _eyePosW;

        private float _radius;
        private float _theta;
        private float _phi;

        private Point _lastMousePos;
        private bool _disposed;
        public MirrorDemo(IntPtr hInstance) : base(hInstance) {
            _skullTranslation = new Vector3(0, 1, -5);
            _eyePosW = new Vector3();
            _renderOptions = RenderOptions.Textures;
            _theta = 1.24f * MathF.PI;
            _phi = 0.42f * MathF.PI;
            _radius = 12.0f;

            MainWindowCaption = "Mirror Demo";

            Enable4xMsaa = false;

            _lastMousePos = new Point();

            _roomWorld = Matrix.Identity;
            _view = Matrix.Identity;
            _proj = Matrix.Identity;

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

            _roomMat = new Material {
                Ambient = new Color4(0.5f, 0.5f, 0.5f), 
                Diffuse = Color.White, 
                Specular = new Color4(16.0f, 0.4f, 0.4f, 0.4f)
            };

            _skullMat = new Material {
                Ambient = new Color4(0.5f, 0.5f, 0.5f),
                Diffuse = Color.White,
                Specular = new Color4(16.0f, 0.4f, 0.4f, 0.4f)
            };
            _mirrorMat = new Material {
                Ambient = new Color4(0.5f, 0.5f, 0.5f),
                Diffuse = new Color4(0.5f, 1,1,1),
                Specular = new Color4(16.0f, 0.4f, 0.4f, 0.4f)
            };
            _shadowMat = new Material {
                Ambient = Color.Black,
                Diffuse = new Color4(0.5f, 0,0,0),
                Specular = new Color4(16.0f, 0, 0, 0)
            };

        }
        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    ImmediateContext.ClearState();
                    Util.ReleaseCom(ref _roomVB);
                    Util.ReleaseCom(ref _skullVB);
                    Util.ReleaseCom(ref _skullIB);
                    Util.ReleaseCom(ref _floorDiffuseMapSRV);
                    Util.ReleaseCom(ref _wallDiffuseMapSRV);
                    Util.ReleaseCom(ref _mirrorDiffuseMapSRV);

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

            _floorDiffuseMapSRV = ShaderResourceView.FromFile(Device, "Textures/checkboard.dds");
            _wallDiffuseMapSRV = ShaderResourceView.FromFile(Device, "Textures/brick01.dds");
            _mirrorDiffuseMapSRV = ShaderResourceView.FromFile(Device, "Textures/ice.dds");

            BuildRoomGeometryBuffers();
            BuildSkullGeometryBuffers();

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
            }
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
            _eyePosW = new Vector3(x, y, z);

            // Build the view matrix
            var pos = new Vector3(x, y, z);
            var target = new Vector3(0);
            var up = new Vector3(0, 1, 0);
            _view = Matrix.LookAtLH(pos, target, up);

            if (Util.IsKeyDown(Keys.Left)) {
                _skullTranslation.X += -1.0f * dt;
            }
            if (Util.IsKeyDown(Keys.Right)) {
                _skullTranslation.X += 1.0f * dt;
            }
            if (Util.IsKeyDown(Keys.Up)) {
                _skullTranslation.Y += 1.0f * dt;
            }
            if (Util.IsKeyDown(Keys.Down)) {
                _skullTranslation.Y += -1.0f * dt;
            }
            _skullTranslation.Y = Math.Max(_skullTranslation.Y, 0);

            var skullRotate = Matrix.RotationY(0.5f * MathF.PI);
            var skullScale = Matrix.Scaling(0.45f, 0.45f, 0.45f);
            var skullOffset = Matrix.Translation(_skullTranslation);

            _skullWorld = skullRotate * skullScale * skullOffset;
        }

        public override void DrawScene() {
            base.DrawScene();
            ImmediateContext.ClearRenderTargetView(RenderTargetView, Color.Black);
            ImmediateContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);

            ImmediateContext.InputAssembler.InputLayout = InputLayouts.Basic32;
            ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            var blendFactor = new Color4(0, 0, 0, 0);

            var viewProj = _view * _proj;

            Effects.BasicFX.SetDirLights(_dirLights);
            Effects.BasicFX.SetEyePosW(_eyePosW);

            Effects.BasicFX.SetFogColor(Color.Black);
            Effects.BasicFX.SetFogStart(2.0f);
            Effects.BasicFX.SetFogRange(40.0f);

            EffectTechnique activeTech;
            EffectTechnique activeSkullTech;

            
            switch (_renderOptions) {
                case RenderOptions.Lighting:
                    activeTech = Effects.BasicFX.Light3Tech;
                    activeSkullTech = Effects.BasicFX.Light3Tech;
                    break;
                case RenderOptions.Textures:
                    activeTech = Effects.BasicFX.Light3TexTech;
                    activeSkullTech = Effects.BasicFX.Light3Tech;
                    break;
                case RenderOptions.TexturesAndFog:
                    activeTech = Effects.BasicFX.Light3TexFogTech;
                    activeSkullTech = Effects.BasicFX.Light3FogTech;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            DrawRoom(activeTech, viewProj);
            DrawSkull(activeSkullTech, viewProj);
            MarkMirrorOnStencil(activeTech, viewProj, blendFactor);
            DrawFloorReflection(activeTech, viewProj);
            DrawSkullReflection(activeSkullTech, viewProj);
            
            DrawSkullShadowReflection(activeSkullTech, viewProj, blendFactor);
            DrawMirror(activeTech, viewProj, blendFactor);

            ImmediateContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Stencil, 1.0f, 0);

            DrawSkullShadow(activeSkullTech, viewProj, blendFactor);
            SwapChain.Present(0, PresentFlags.None);

        }

        private void DrawRoom(EffectTechnique activeTech, Matrix viewProj) {
            // Draw floor and walls
            for (int p = 0; p < activeTech.Description.PassCount; p ++) {
                var pass = activeTech.GetPassByIndex(p);

                ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_roomVB, Basic32.Stride, 0));

                var world = _roomWorld;
                var wit = MathF.InverseTranspose(world);
                var wvp = world * viewProj;

                Effects.BasicFX.SetWorld(world);
                Effects.BasicFX.SetWorldInvTranspose(wit);
                Effects.BasicFX.SetWorldViewProj(wvp);
                Effects.BasicFX.SetTexTransform(Matrix.Identity);
                Effects.BasicFX.SetMaterial(_roomMat);

                Effects.BasicFX.SetDiffuseMap(_floorDiffuseMapSRV);
                pass.Apply(ImmediateContext);
                ImmediateContext.Draw(6, 0);

                Effects.BasicFX.SetDiffuseMap(_wallDiffuseMapSRV);
                pass.Apply(ImmediateContext);
                ImmediateContext.Draw(18, 6);
            }
        }

        private void DrawSkull(EffectTechnique activeSkullTech, Matrix viewProj) {
            // Draw skull
            for (int p = 0; p < activeSkullTech.Description.PassCount; p ++) {
                var pass = activeSkullTech.GetPassByIndex(p);

                ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_skullVB, Basic32.Stride, 0));
                ImmediateContext.InputAssembler.SetIndexBuffer(_skullIB, Format.R32_UInt, 0);

                var world = _skullWorld;
                var wit = MathF.InverseTranspose(world);
                var wvp = world * viewProj;

                Effects.BasicFX.SetWorld(world);
                Effects.BasicFX.SetWorldInvTranspose(wit);
                Effects.BasicFX.SetWorldViewProj(wvp);
                Effects.BasicFX.SetMaterial(_skullMat);

                pass.Apply(ImmediateContext);
                ImmediateContext.DrawIndexed(_skullIndexCount, 0, 0);
            }
        }

        private void MarkMirrorOnStencil(EffectTechnique activeTech, Matrix viewProj, Color4 blendFactor) {
            // Draw mirror to stencil
            for (int p = 0; p < activeTech.Description.PassCount; p++) {
                var pass = activeTech.GetPassByIndex(p);

                ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_roomVB, Basic32.Stride, 0));

                var world = _roomWorld;
                var wit = MathF.InverseTranspose(world);
                var wvp = world * viewProj;

                Effects.BasicFX.SetWorld(world);
                Effects.BasicFX.SetWorldInvTranspose(wit);
                Effects.BasicFX.SetWorldViewProj(wvp);
                Effects.BasicFX.SetTexTransform(Matrix.Identity);

                ImmediateContext.OutputMerger.BlendState = RenderStates.NoRenderTargetWritesBS;
                ImmediateContext.OutputMerger.BlendFactor = blendFactor;
                ImmediateContext.OutputMerger.BlendSampleMask = -1;

                ImmediateContext.OutputMerger.DepthStencilState = RenderStates.MarkMirrorDSS;
                ImmediateContext.OutputMerger.DepthStencilReference = 1;

                pass.Apply(ImmediateContext);
                ImmediateContext.Draw(6, 24);
                ImmediateContext.OutputMerger.DepthStencilState = null;
                ImmediateContext.OutputMerger.DepthStencilReference = 0;
                ImmediateContext.OutputMerger.BlendState = null;
                ImmediateContext.OutputMerger.BlendFactor = blendFactor;
                ImmediateContext.OutputMerger.BlendSampleMask = -1;
            }
        }

        private void DrawFloorReflection(EffectTechnique activeTech, Matrix viewProj) {
            for (int p = 0; p < activeTech.Description.PassCount; p++) {
                var pass = activeTech.GetPassByIndex(p);

                ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_roomVB, Basic32.Stride, 0));

                var mirrorPlane = new Plane(new Vector3(0, 0, 1), 0);
                var r = Matrix.Reflection(mirrorPlane);

                var world = _roomWorld * r;
                var wit = MathF.InverseTranspose(world);
                var wvp = world * viewProj;

                Effects.BasicFX.SetWorld(world);
                Effects.BasicFX.SetWorldInvTranspose(wit);
                Effects.BasicFX.SetWorldViewProj(wvp);
                Effects.BasicFX.SetTexTransform(Matrix.Identity);
                Effects.BasicFX.SetMaterial(_roomMat);
                Effects.BasicFX.SetDiffuseMap(_floorDiffuseMapSRV);

                var oldLightDirections = _dirLights.Select(l => l.Direction).ToArray();

                for (int i = 0; i < _dirLights.Length; i++) {
                    var l = _dirLights[i];
                    var lightDir = l.Direction;
                    var reflectedLightDir = Vector3.Transform(lightDir, r);
                    _dirLights[i].Direction = new Vector3(reflectedLightDir.X, reflectedLightDir.Y, reflectedLightDir.Z);
                }
                Effects.BasicFX.SetDirLights(_dirLights);

                ImmediateContext.Rasterizer.State = RenderStates.CullClockwiseRS;

                ImmediateContext.OutputMerger.DepthStencilState = RenderStates.DrawReflectionDSS;
                ImmediateContext.OutputMerger.DepthStencilReference = 1;
                pass.Apply(ImmediateContext);

                ImmediateContext.Draw(6, 0);

                ImmediateContext.Rasterizer.State = null;
                ImmediateContext.OutputMerger.DepthStencilState = null;
                ImmediateContext.OutputMerger.DepthStencilReference = 0;

                for (int i = 0; i < oldLightDirections.Length; i++) {
                    _dirLights[i].Direction = oldLightDirections[i];
                }
                Effects.BasicFX.SetDirLights(_dirLights);
            }
        }

        private void DrawSkullReflection(EffectTechnique activeSkullTech, Matrix viewProj) {
            // Draw skull reflection

            for (int p = 0; p < activeSkullTech.Description.PassCount; p++) {
                var pass = activeSkullTech.GetPassByIndex(p);

                ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_skullVB, Basic32.Stride, 0));
                ImmediateContext.InputAssembler.SetIndexBuffer(_skullIB, Format.R32_UInt, 0);

                var mirrorPlane = new Plane(new Vector3(0, 0, 1), 0);
                var r = Matrix.Reflection(mirrorPlane);

                var world = _skullWorld * r;
                var wit = MathF.InverseTranspose(world);
                var wvp = world * viewProj;

                Effects.BasicFX.SetWorld(world);
                Effects.BasicFX.SetWorldInvTranspose(wit);
                Effects.BasicFX.SetWorldViewProj(wvp);
                Effects.BasicFX.SetMaterial(_skullMat);

                var oldLightDirections = _dirLights.Select(l => l.Direction).ToArray();

                for (int i = 0; i < _dirLights.Length; i++) {
                    var l = _dirLights[i];
                    var lightDir = l.Direction;
                    var reflectedLightDir = Vector3.Transform(lightDir, r);
                    _dirLights[i].Direction = new Vector3(reflectedLightDir.X, reflectedLightDir.Y, reflectedLightDir.Z);
                }
                Effects.BasicFX.SetDirLights(_dirLights);

                ImmediateContext.Rasterizer.State = RenderStates.CullClockwiseRS;

                ImmediateContext.OutputMerger.DepthStencilState = RenderStates.DrawReflectionDSS;
                ImmediateContext.OutputMerger.DepthStencilReference = 1;
                pass.Apply(ImmediateContext);

                ImmediateContext.DrawIndexed(_skullIndexCount, 0, 0);

                ImmediateContext.Rasterizer.State = null;
                ImmediateContext.OutputMerger.DepthStencilState = null;
                ImmediateContext.OutputMerger.DepthStencilReference = 0;

                for (int i = 0; i < oldLightDirections.Length; i++) {
                    _dirLights[i].Direction = oldLightDirections[i];
                }
                Effects.BasicFX.SetDirLights(_dirLights);
            }
        }

        private void DrawSkullShadowReflection(EffectTechnique activeSkullTech, Matrix viewProj, Color4 blendFactor) {
            // draw skull shadow
            for (int p = 0; p < activeSkullTech.Description.PassCount; p++) {
                var pass = activeSkullTech.GetPassByIndex(p);

                ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_skullVB, Basic32.Stride, 0));
                ImmediateContext.InputAssembler.SetIndexBuffer(_skullIB, Format.R32_UInt, 0);

                var shadowPlane = new Plane(new Vector3(0, 1, 0), 0.0f);
                var toMainLight = -_dirLights[0].Direction;

                var s = Matrix.Shadow(new Vector4(toMainLight, 0), shadowPlane);
                var shadowOffsetY = Matrix.Translation(0, 0.001f, 0);

                var mirrorPlane = new Plane(new Vector3(0, 0, 1), 0);
                var r = Matrix.Reflection(mirrorPlane);

                var world = _skullWorld * s * shadowOffsetY * r;
                var wit = MathF.InverseTranspose(world);
                var wvp = world * viewProj;

                Effects.BasicFX.SetWorld(world);
                Effects.BasicFX.SetWorldInvTranspose(wit);
                Effects.BasicFX.SetWorldViewProj(wvp);
                Effects.BasicFX.SetMaterial(_shadowMat);

                ImmediateContext.OutputMerger.BlendState = RenderStates.TransparentBS;
                ImmediateContext.OutputMerger.BlendFactor = blendFactor;
                ImmediateContext.OutputMerger.BlendSampleMask = -1;

                var oldLightDirections = _dirLights.Select(l => l.Direction).ToArray();

                for (int i = 0; i < _dirLights.Length; i++) {
                    var l = _dirLights[i];
                    var lightDir = l.Direction;
                    var reflectedLightDir = Vector3.Transform(lightDir, r);
                    _dirLights[i].Direction = new Vector3(reflectedLightDir.X, reflectedLightDir.Y, reflectedLightDir.Z);
                }
                Effects.BasicFX.SetDirLights(_dirLights);

                ImmediateContext.Rasterizer.State = RenderStates.CullClockwiseRS;

                ImmediateContext.OutputMerger.DepthStencilState = RenderStates.NoDoubleBlendDSS;
                ImmediateContext.OutputMerger.DepthStencilReference = 1;
                pass.Apply(ImmediateContext);

                ImmediateContext.DrawIndexed(_skullIndexCount, 0, 0);

                ImmediateContext.Rasterizer.State = null;
                ImmediateContext.OutputMerger.DepthStencilState = null;
                ImmediateContext.OutputMerger.DepthStencilReference = 0;

                ImmediateContext.OutputMerger.BlendState = null;
                ImmediateContext.OutputMerger.BlendFactor = blendFactor;
                ImmediateContext.OutputMerger.BlendSampleMask = -1;

                for (int i = 0; i < oldLightDirections.Length; i++) {
                    _dirLights[i].Direction = oldLightDirections[i];
                }
                Effects.BasicFX.SetDirLights(_dirLights);
            }
        }

        private void DrawMirror(EffectTechnique activeTech, Matrix viewProj, Color4 blendFactor) {
            // draw mirror with transparency
            for (int p = 0; p < activeTech.Description.PassCount; p++) {
                var pass = activeTech.GetPassByIndex(p);

                ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_roomVB, Basic32.Stride, 0));

                var world = _roomWorld;
                var wit = MathF.InverseTranspose(world);
                var wvp = world * viewProj;

                Effects.BasicFX.SetWorld(world);
                Effects.BasicFX.SetWorldInvTranspose(wit);
                Effects.BasicFX.SetWorldViewProj(wvp);
                Effects.BasicFX.SetTexTransform(Matrix.Identity);
                Effects.BasicFX.SetMaterial(_mirrorMat);
                Effects.BasicFX.SetDiffuseMap(_mirrorDiffuseMapSRV);

                ImmediateContext.OutputMerger.BlendState = RenderStates.TransparentBS;
                ImmediateContext.OutputMerger.BlendFactor = blendFactor;
                ImmediateContext.OutputMerger.BlendSampleMask = -1;

                pass.Apply(ImmediateContext);
                ImmediateContext.Draw(6, 24);
            }
        }

        private void DrawSkullShadow(EffectTechnique activeSkullTech, Matrix viewProj, Color4 blendFactor) {
            // draw skull shadow on floor
            for (int p = 0; p < activeSkullTech.Description.PassCount; p++) {
                var pass = activeSkullTech.GetPassByIndex(p);

                ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_skullVB, Basic32.Stride, 0));
                ImmediateContext.InputAssembler.SetIndexBuffer(_skullIB, Format.R32_UInt, 0);

                var shadowPlane = new Plane(new Vector3(0, 1, 0), 0.0f);
                var toMainLight = -_dirLights[0].Direction;

                var s = Matrix.Shadow(new Vector4(toMainLight, 0), shadowPlane);
                var shadowOffsetY = Matrix.Translation(0, 0.001f, 0);

                var world = _skullWorld * s * shadowOffsetY;
                var wit = MathF.InverseTranspose(world);
                var wvp = world * viewProj;

                Effects.BasicFX.SetWorld(world);
                Effects.BasicFX.SetWorldInvTranspose(wit);
                Effects.BasicFX.SetWorldViewProj(wvp);
                Effects.BasicFX.SetMaterial(_shadowMat);

                ImmediateContext.OutputMerger.BlendState = RenderStates.TransparentBS;
                ImmediateContext.OutputMerger.BlendFactor = blendFactor;
                ImmediateContext.OutputMerger.BlendSampleMask = -1;

                ImmediateContext.OutputMerger.DepthStencilState = RenderStates.NoDoubleBlendDSS;
                ImmediateContext.OutputMerger.DepthStencilReference = 0;
                pass.Apply(ImmediateContext);

                ImmediateContext.DrawIndexed(_skullIndexCount, 0, 0);

                // draw skull shadow on wall
                shadowPlane = new Plane(new Vector3(0, 0, -1), 0.0f);
                toMainLight = -_dirLights[0].Direction;

                s = Matrix.Shadow(new Vector4(toMainLight, 0), shadowPlane);
                shadowOffsetY = Matrix.Translation(0, 0, -0.001f);

                world = _skullWorld * s * shadowOffsetY;
                wit = MathF.InverseTranspose(world);
                wvp = world * viewProj;

                Effects.BasicFX.SetWorld(world);
                Effects.BasicFX.SetWorldInvTranspose(wit);
                Effects.BasicFX.SetWorldViewProj(wvp);
                Effects.BasicFX.SetMaterial(_shadowMat);

                pass.Apply(ImmediateContext);

                ImmediateContext.DrawIndexed(_skullIndexCount, 0, 0);

                ImmediateContext.Rasterizer.State = null;
                ImmediateContext.OutputMerger.DepthStencilState = null;
                ImmediateContext.OutputMerger.DepthStencilReference = 0;

                ImmediateContext.OutputMerger.BlendState = null;
                ImmediateContext.OutputMerger.BlendFactor = blendFactor;
                ImmediateContext.OutputMerger.BlendSampleMask = -1;
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

                _theta += dx;
                _phi += dy;

                _phi = MathF.Clamp(_phi, 0.1f, MathF.PI - 0.1f);
            } else if (e.Button == MouseButtons.Right) {
                var dx = 0.01f * (e.X - _lastMousePos.X);
                var dy = 0.01f * (e.Y - _lastMousePos.Y);
                _radius += dx - dy;

                _radius = MathF.Clamp(_radius, 3.0f, 50.0f);
            }
            _lastMousePos = e.Location;
        }

        private void BuildRoomGeometryBuffers() {
            var v = new Basic32[30];

            // Floor
            v[0] = new Basic32(new Vector3(-3.5f, 0, -10), new Vector3(0,1,0), new Vector2(0, 4)  );
            v[1] = new Basic32(new Vector3(-3.5f, 0, 0), new Vector3(0, 1, 0), new Vector2(0, 0));
            v[2] = new Basic32(new Vector3(7.5f, 0, 0), new Vector3(0, 1, 0), new Vector2(4, 0)); 
            
            v[3] = new Basic32(new Vector3(-3.5f, 0, -10), new Vector3(0, 1, 0), new Vector2(0, 4));
            v[4] = new Basic32(new Vector3(7.5f, 0, 0), new Vector3(0, 1, 0), new Vector2(4, 0)); 
            v[5] = new Basic32(new Vector3(7.5f, 0, -10), new Vector3(0, 1, 0), new Vector2(4, 4));

            // Wall
            v[6] = new Basic32(new Vector3(-3.5f, 0, 0), new Vector3(0,  0, -1), new Vector2(0, 2));
            v[7] = new Basic32(new Vector3(-3.5f, 4, 0), new Vector3(0,  0, -1), new Vector2(0, 0));
            v[8] = new Basic32(new Vector3(-2.5f, 4, 0), new Vector3(0, 0, -1), new Vector2(0.5f, 0));

            v[9] = new Basic32(new Vector3(-3.5f, 0, 0), new Vector3(0, 0, -1), new Vector2(0, 2));
            v[10] = new Basic32(new Vector3(-2.5f, 4, 0), new Vector3(0, 0, -1), new Vector2(0.5f, 0));
            v[11] = new Basic32(new Vector3(-2.5f, 0, 0), new Vector3(0, 0, -1), new Vector2(0.5f, 2.0f));

            v[12] = new Basic32(new Vector3(2.5f, 0, 0), new Vector3(0, 0, -1), new Vector2(0, 2));
            v[13] = new Basic32(new Vector3(2.5f, 4, 0), new Vector3(0, 0, -1), new Vector2(0, 0));
            v[14] = new Basic32(new Vector3(7.5f, 4, 0), new Vector3(0, 0, -1), new Vector2(2, 0));

            v[15] = new Basic32(new Vector3(2.5f, 0, 0), new Vector3(0, 0, -1), new Vector2(0, 2));
            v[16] = new Basic32(new Vector3(7.5f, 4, 0), new Vector3(0, 0, -1), new Vector2(2, 0));
            v[17] = new Basic32(new Vector3(7.5f, 0, 0), new Vector3(0, 0, -1), new Vector2(2, 2));

            v[18] = new Basic32(new Vector3(-3.5f, 4.0f, 0), new Vector3(0, 0, -1), new Vector2(0, 1));
            v[19] = new Basic32(new Vector3(-3.5f, 6, 0), new Vector3(0, 0, -1), new Vector2(0, 0));
            v[20] = new Basic32(new Vector3(7.5f, 6, 0), new Vector3(0, 0, -1), new Vector2(6, 0));

            v[21] = new Basic32(new Vector3(-3.5f, 4, 0), new Vector3(0, 0, -1), new Vector2(0, 1));
            v[22] = new Basic32(new Vector3(7.5f, 6, 0), new Vector3(0, 0, -1), new Vector2(6, 0));
            v[23] = new Basic32(new Vector3(7.5f, 4, 0), new Vector3(0, 0, -1), new Vector2(6, 1));

            // Mirror
            v[24] = new Basic32(new Vector3(-2.5f, 0, 0), new Vector3(0, 0, -1), new Vector2(0, 1));
            v[25] = new Basic32(new Vector3(-2.5f, 4, 0), new Vector3(0, 0, -1), new Vector2(0, 0));
            v[26] = new Basic32(new Vector3(2.5f, 4, 0), new Vector3(0, 0, -1), new Vector2(1, 0));

            v[27] = new Basic32(new Vector3(-2.5f, 0, 0), new Vector3(0, 0, -1), new Vector2(0, 1));
            v[28] = new Basic32(new Vector3(2.5f, 4, 0), new Vector3(0, 0, -1), new Vector2(1, 0));
            v[29] = new Basic32(new Vector3(2.5f, 0, 0), new Vector3(0, 0, -1), new Vector2(1, 1));

            var vbd = new BufferDescription {
                Usage = ResourceUsage.Immutable,
                SizeInBytes = Basic32.Stride * 30,
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None,
                StructureByteStride = 0
            };
            _roomVB= new Buffer(Device, new DataStream(v.ToArray(), false, false), vbd);
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
    }

    class Program {
        static void Main(string[] args) {
            Configuration.EnableObjectTracking = true;
            var app = new MirrorDemo(Process.GetCurrentProcess().Handle);
            if (!app.Init()) {
                return;
            }
            app.Run();
        }
    }
}
