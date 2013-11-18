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

    public class Minimap :DisposableClass {
        private readonly DeviceContext _dc;

        private RenderTargetView _minimapRTV;
        public ShaderResourceView MinimapSRV { get; private set; }
        private readonly Viewport _minimapViewport;
        private readonly Terrain _terrain;
        private readonly OrthoCamera _camera = new OrthoCamera();
        private readonly CameraBase _viewCam;

        private Buffer _frustumVB;
        private readonly Plane[] _edgePlanes;
        private bool _disposed;

        public Vector2 ScreenPosition { get; set; }
        public Vector2 Size { get; set; }

        public Minimap(Device device, DeviceContext dc, int minimapWidth, int minimapHeight, Terrain terrain, CameraBase viewCam) {
            _dc = dc;

            _minimapViewport = new Viewport(0, 0, minimapWidth, minimapHeight);

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

            _terrain = terrain;

            _camera.Target = new Vector3(0,0,0);//new Vector3(_terrain.Width/2, 0, _terrain.Depth/2);
            _camera.Position = new Vector3(0, _terrain.Depth, 0);//new Vector3(_terrain.Width / 2, 1000, _terrain.Depth / 2);
            _camera.SetLens(_terrain.Width, _terrain.Depth);

            _viewCam = viewCam;

            var vbd = new BufferDescription(VertexPC.Stride * 5, ResourceUsage.Dynamic, BindFlags.VertexBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
            _frustumVB = new Buffer(device, vbd);

            _edgePlanes = new[] {
                new Plane(1, 0, 0, -_terrain.Width / 2),
                new Plane(-1, 0, 0, _terrain.Width / 2),
                new Plane(0, 1, 0, -_terrain.Depth / 2),
                new Plane(0, -1, 0, _terrain.Depth / 2)
            };

            ScreenPosition = new Vector2( 0.25f, 0.75f);
            Size = new Vector2(0.25f, 0.25f);
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
            _terrain.Draw(_dc, _camera, lights);

            DrawCameraFrustum();
        }

        private void DrawCameraFrustum() {
            var view = _viewCam.View;

            var fovX = _viewCam.FovX;
            var fovY = _viewCam.FovY;
            var org = new Vector3();
            var dirs = new[] {
                new Vector3(fovX, fovY, 1.0f),
                new Vector3(-fovX, fovY, 1.0f),
                new Vector3(-fovX, -fovY, 1.0f),
                new Vector3(fovX, -fovY, 1.0f)
            };
            var points = new Vector3[4];

            var invView = Matrix.Invert(view);
            org = Vector3.TransformCoordinate(org, invView);

            var groundPlane = new Plane(new Vector3(), new Vector3(0, 1, 0));
            var ok = true;
            for (var i = 0; i < 4 && ok; i++) {
                dirs[i] = Vector3.Normalize(Vector3.TransformNormal(dirs[i], invView));

                dirs[i] *= 100000.0f;

                Vector3 hit;
                if (!Plane.Intersects(groundPlane, org, dirs[i], out hit)) {
                    ok = false;
                }
                var n = _viewCam.FrustumPlanes[Frustum.Near];
                var d = n.Normal.X * hit.X + n.Normal.Y * hit.Y + n.Normal.Z * hit.Z + n.D;
                if (d < 0.0f) {
                    ok = false;
                    foreach (var edgePlane in _edgePlanes) {
                        if (!Plane.Intersects(edgePlane, org, dirs[i], out hit)) {
                            continue;
                        }
                        d = n.Normal.X * hit.X + n.Normal.Y * hit.Y + n.Normal.Z * hit.Z + n.D;
                        if (!(d >= 0.0f)) {
                            continue;
                        }
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
            var buf = _dc.MapSubresource(_frustumVB, MapMode.WriteDiscard, MapFlags.None);

            buf.Data.Write(new VertexPC(points[0], Color.White));
            buf.Data.Write(new VertexPC(points[1], Color.White));
            buf.Data.Write(new VertexPC(points[2], Color.White));
            buf.Data.Write(new VertexPC(points[3], Color.White));
            buf.Data.Write(new VertexPC(points[0], Color.White));

            _dc.UnmapSubresource(_frustumVB, 0);

            _dc.InputAssembler.InputLayout = InputLayouts.PosColor;
            _dc.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineStrip;
            _dc.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_frustumVB, VertexPC.Stride, 0));
            for (var i = 0; i < Effects.ColorFX.ColorTech.Description.PassCount; i++) {
                Effects.ColorFX.SetWorldViewProj(_camera.ViewProj);
                Effects.ColorFX.ColorTech.GetPassByIndex(i).Apply(_dc);
                _dc.Draw(5, 0);
            }
        }

        public void Draw( DeviceContext dc) {
            var stride = Basic32.Stride;
            const int Offset = 0;

            dc.InputAssembler.InputLayout = InputLayouts.Basic32;
            dc.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            dc.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(D3DApp.GD3DApp.ScreenQuadVB, stride, Offset));
            dc.InputAssembler.SetIndexBuffer(D3DApp.GD3DApp.ScreenQuadIB, Format.R32_UInt, 0);

            var world = new Matrix {
                M11 = Size.X,
                M22 = Size.Y,
                M33 = 1.0f,
                M41 = -1.0f + 2*ScreenPosition.X + (Size.X ),
                M42 = 1.0f - 2*ScreenPosition.Y - (Size.Y ),
                M44 = 1.0f
            };



            var tech = Effects.DebugTexFX.ViewArgbTech;
            for (int p = 0; p < tech.Description.PassCount; p++) {
                Effects.DebugTexFX.SetWorldViewProj(world);
                Effects.DebugTexFX.SetTexture(MinimapSRV);
                tech.GetPassByIndex(p).Apply(dc);
                dc.DrawIndexed(6, 0, 0);
            }
        }

        public bool Contains(ref Vector2 p) {
            if (p.X >= ScreenPosition.X && p.X <= ScreenPosition.X + Size.X &&
                p.Y >= ScreenPosition.Y && p.Y <= ScreenPosition.Y + Size.Y) {

                p.X = (p.X - ScreenPosition.X) / Size.X;
                p.Y = (p.Y - ScreenPosition.Y) / Size.Y;

                return true;
            }
            return false;
        }
    }
}