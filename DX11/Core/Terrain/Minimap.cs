using System.Windows.Forms;

namespace Core.Terrain {
    using System;
    using System.Drawing;

    using Core;
    using Core.Camera;
    using Core.FX;
    using Core.Vertex;

    using SlimDX;
    using SlimDX.Direct3D11;
    using SlimDX.DXGI;

    using Buffer = SlimDX.Direct3D11.Buffer;
    using Device = SlimDX.Direct3D11.Device;
    using MapFlags = SlimDX.Direct3D11.MapFlags;

    public class Minimap : DisposableClass {
        // DeviceContext to render with
        private readonly DeviceContext _dc;

        // RenderTarget and ShaderResource views of the minimap texture
        private RenderTargetView _minimapRTV;
        public ShaderResourceView MinimapSRV { get; private set; }

        // viewport to match the minimap texture size
        private readonly Viewport _minimapViewport;
        // reference to the terrain that we render in the minimap
        private readonly Terrain _terrain;

        // ortho camera for rendering the minimap
        private readonly OrthoCamera _orthoCamera = new OrthoCamera();
        // reference to the normal view camera, so we can render the view frustum on the minimap
        private readonly CameraBase _viewCam;

        // vertex buffer to hold the view camera frustum points
        private Buffer _frustumVB;
        // array of planes defining the "box" that surrounds the terrain
        private readonly Plane[] _edgePlanes;
        private bool _disposed;

        // position and size of the minimap when rendered to our backbuffer
        // these are defined in percentages of the full backbuffer dimensions, to scale when the screen size changes
        public Vector2 ScreenPosition { get; set; }
        public Vector2 Size { get; set; }

        public Minimap(Device device, DeviceContext dc, int minimapWidth, int minimapHeight, Terrain terrain, CameraBase viewCam) {
            _dc = dc;

            _minimapViewport = new Viewport(0, 0, minimapWidth, minimapHeight);

            CreateMinimapTextureViews(device, minimapWidth, minimapHeight);

            _terrain = terrain;

            SetupOrthoCamera();
            _viewCam = viewCam;

            // frustum vb will contain four corners of view frustum, with first vertex repeated as the last
            var vbd = new BufferDescription(
                VertexPC.Stride * 5,
                ResourceUsage.Dynamic,
                BindFlags.VertexBuffer,
                CpuAccessFlags.Write,
                ResourceOptionFlags.None,
                0
            );
            _frustumVB = new Buffer(device, vbd);

            _edgePlanes = new[] {
                new Plane(1, 0, 0, -_terrain.Width / 2),
                new Plane(-1, 0, 0, _terrain.Width / 2),
                new Plane(0, 1, 0, -_terrain.Depth / 2),
                new Plane(0, -1, 0, _terrain.Depth / 2)
            };

            ScreenPosition = new Vector2(0.25f, 0.75f);
            Size = new Vector2(0.25f, 0.25f);
        }

        private void SetupOrthoCamera() {
            _orthoCamera.Target = new Vector3(0, 0, 0);
            _orthoCamera.Position = new Vector3(0, _terrain.Depth, 0);
            _orthoCamera.SetLens(_terrain.Width, _terrain.Depth, 1, _terrain.Depth * 2);
            _orthoCamera.UpdateViewMatrix();
        }

        private void CreateMinimapTextureViews(Device device, int minimapWidth, int minimapHeight) {
            var texDesc = new Texture2DDescription {
                ArraySize = 1,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = Format.R8G8B8A8_UNorm,
                Width = minimapWidth,
                Height = minimapHeight,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default
            };

            var tex = new Texture2D(device, texDesc);

            _minimapRTV = new RenderTargetView(device, tex);
            MinimapSRV = new ShaderResourceView(device, tex);
            Util.ReleaseCom(ref tex);
        }

        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Util.ReleaseCom(ref _frustumVB);
                    Util.ReleaseCom(ref _minimapRTV);
                    var shaderResourceView = MinimapSRV;
                    Util.ReleaseCom(ref shaderResourceView);
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }


        public void RenderMinimap(DirectionalLight[] lights) {
            _dc.OutputMerger.SetTargets((DepthStencilView)null, _minimapRTV);
            _dc.Rasterizer.SetViewports(_minimapViewport);

            _dc.ClearRenderTargetView(_minimapRTV, Color.White);
            _terrain.Renderer.Draw(_dc, _orthoCamera, lights);

            DrawCameraFrustum();
        }

        public void OnClick(MouseEventArgs e) {
            var x = (float)e.X / D3DApp.GD3DApp.Window.ClientSize.Width;
            var y = (float)e.Y / D3DApp.GD3DApp.Window.ClientSize.Height;
            var p = new Vector2(x, y);
            if (Contains(ref p)) {
                // convert minimap-space to world-space and set the camera target
                var terrainX = _terrain.Width * p.X - _terrain.Width / 2;
                var terrainZ = -_terrain.Depth * p.Y + _terrain.Depth / 2;
                var cam = _viewCam as LookAtCamera;
                if (cam != null) {
                    cam.Target = new Vector3(terrainX, _terrain.Height(terrainX, terrainZ), terrainZ);
                }
            }
        }

        private void DrawCameraFrustum() {
            var view = _viewCam.View;

            var fovX = _viewCam.FovX;
            var fovY = _viewCam.FovY;
            // world-space camera position
            var org = _viewCam.Position;

            // vectors pointed towards the corners of the near plane of the view frustum
            var dirs = new[] {
                new Vector3(fovX, fovY, 1.0f),
                new Vector3(-fovX, fovY, 1.0f),
                new Vector3(-fovX, -fovY, 1.0f),
                new Vector3(fovX, -fovY, 1.0f)
            };
            var points = new Vector3[4];

            // view-to-world transform
            var invView = Matrix.Invert(view);

            // XZ plane
            var groundPlane = new Plane(new Vector3(), new Vector3(0, 1, 0));

            var ok = true;
            for (var i = 0; i < 4 && ok; i++) {
                // transform the view-space vector into world-space
                dirs[i] = Vector3.Normalize(Vector3.TransformNormal(dirs[i], invView));
                // extend the near-plane vectors into very far away points
                dirs[i] *= 100000.0f;

                Vector3 hit;
                // check if the ray between the camera origin and the far point intersects the ground plane
                if (!Plane.Intersects(groundPlane, org, dirs[i], out hit)) {
                    ok = false;
                }
                // make sure that the intersection is on the positive side of the frustum near plane
                var n = _viewCam.FrustumPlanes[Frustum.Near];
                var d = n.Normal.X * hit.X + n.Normal.Y * hit.Y + n.Normal.Z * hit.Z + n.D;
                if (d < 0.0f) {
                    ok = false;
                    // if we're here, the ray was pointing away from the ground
                    // so we will instead intersect the ray with the terrain boundary planes
                    foreach (var edgePlane in _edgePlanes) {
                        if (!Plane.Intersects(edgePlane, org, dirs[i], out hit)) {
                            continue;
                        }
                        d = n.Normal.X * hit.X + n.Normal.Y * hit.Y + n.Normal.Z * hit.Z + n.D;
                        if (!(d >= 0.0f)) {
                            continue;
                        }
                        // bump out the intersection point, so that if we're looking into the corners, the
                        // frustum doesn't show that we shouldn't be able to see terrain that we can see
                        hit *= 2;
                        ok = true;
                        break;
                    }
                }
                points[i] = new Vector3(Math.Min(Math.Max(hit.X, -float.MaxValue), float.MaxValue), 0, Math.Min(Math.Max(hit.Z, -float.MaxValue), float.MaxValue));
            }

            if (!ok) {
                return;
            }

            // update the frustum vertex buffer
            var buf = _dc.MapSubresource(_frustumVB, MapMode.WriteDiscard, MapFlags.None);

            buf.Data.Write(new VertexPC(points[0], Color.White));
            buf.Data.Write(new VertexPC(points[1], Color.White));
            buf.Data.Write(new VertexPC(points[2], Color.White));
            buf.Data.Write(new VertexPC(points[3], Color.White));
            // include the first point twice, to complete the quad when we render as a linestrip
            buf.Data.Write(new VertexPC(points[0], Color.White));

            _dc.UnmapSubresource(_frustumVB, 0);

            _dc.InputAssembler.InputLayout = InputLayouts.PosColor;
            _dc.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineStrip;
            _dc.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_frustumVB, VertexPC.Stride, 0));
            // draw the frustum with a basic position-color shader
            for (var i = 0; i < Effects.ColorFX.ColorTech.Description.PassCount; i++) {
                Effects.ColorFX.SetWorldViewProj(_orthoCamera.ViewProj);
                Effects.ColorFX.ColorTech.GetPassByIndex(i).Apply(_dc);
                _dc.Draw(5, 0);
            }
        }

        public void Draw(DeviceContext dc) {
            var stride = Basic32.Stride;
            const int Offset = 0;

            dc.InputAssembler.InputLayout = InputLayouts.Basic32;
            dc.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            dc.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(D3DApp.GD3DApp.ScreenQuadVB, stride, Offset));
            dc.InputAssembler.SetIndexBuffer(D3DApp.GD3DApp.ScreenQuadIB, Format.R32_UInt, 0);

            // transform from our [0,1] screen cordinates to NDC
            var world = new Matrix {
                M11 = Size.X,
                M22 = Size.Y,
                M33 = 1.0f,
                M41 = -1.0f + 2 * ScreenPosition.X + (Size.X),
                M42 = 1.0f - 2 * ScreenPosition.Y - (Size.Y),
                M44 = 1.0f
            };



            var tech = Effects.DebugTexFX.ViewArgbTech;
            for (var p = 0; p < tech.Description.PassCount; p++) {
                Effects.DebugTexFX.SetWorldViewProj(world);
                Effects.DebugTexFX.SetTexture(MinimapSRV);
                tech.GetPassByIndex(p).Apply(dc);
                dc.DrawIndexed(6, 0, 0);
            }
        }

        public bool Contains(ref Vector2 p) {
            // check if position is within minimap screen bounds
            if (p.X >= ScreenPosition.X && p.X <= ScreenPosition.X + Size.X &&
                p.Y >= ScreenPosition.Y && p.Y <= ScreenPosition.Y + Size.Y) {
                // convert screen-space to minimap-space
                p.X = (p.X - ScreenPosition.X) / Size.X;
                p.Y = (p.Y - ScreenPosition.Y) / Size.Y;

                return true;
            }
            return false;
        }
    }
}