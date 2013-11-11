namespace Minimap {
    using System;
    using System.Drawing;

    using Core;
    using Core.Camera;
    using Core.FX;
    using Core.Terrain;
    using Core.Vertex;

    using SlimDX;
    using SlimDX.Design;
    using SlimDX.DXGI;
    using SlimDX.Direct3D11;

    using Buffer = SlimDX.Direct3D11.Buffer;
    using Device = SlimDX.Direct3D11.Device;
    using MapFlags = SlimDX.Direct3D11.MapFlags;

    public class Minimap {
        private Device _device;
        private DeviceContext _dc;

        private readonly RenderTargetView _minimapRTV;
        public ShaderResourceView MinimapSRV { get; private set; }
        private readonly Viewport _minimapViewport;
        private readonly Terrain _terrain;
        private readonly OrthoCamera _camera = new OrthoCamera();
        private CameraBase _viewCam;

        private Buffer _frustumVB;

        public Minimap(Device device, DeviceContext dc, int minimapWidth, int minimapHeight, Terrain terrain, CameraBase viewCam) {
            _device = device;
            _dc = dc;

            _minimapViewport = new Viewport(0, 0, minimapWidth, minimapHeight);

            var texDesc = new Texture2DDescription() {
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

            var tex = new Texture2D(_device, texDesc);

            _minimapRTV = new RenderTargetView(_device, tex);
            MinimapSRV = new ShaderResourceView(_device, tex);
            Util.ReleaseCom(ref tex);

            _terrain = terrain;

            _camera.Target = new Vector3(0,0,0);//new Vector3(_terrain.Width/2, 0, _terrain.Depth/2);
            _camera.Position = new Vector3(0, 1000, 0);//new Vector3(_terrain.Width / 2, 1000, _terrain.Depth / 2);
            _camera.SetLens(_terrain.Width, _terrain.Depth);

            _viewCam = viewCam;

            var vbd = new BufferDescription(VertexPC.Stride * 8, ResourceUsage.Dynamic, BindFlags.VertexBuffer, CpuAccessFlags.Write, ResourceOptionFlags.None, 0);
            _frustumVB = new Buffer(_device, vbd);

        }

        

        public void RenderMinimap(DirectionalLight[] lights) {

            
            _dc.OutputMerger.SetTargets((DepthStencilView)null, _minimapRTV);
            _dc.Rasterizer.SetViewports(_minimapViewport);

            _dc.ClearRenderTargetView(_minimapRTV, Color.White);
            _terrain.Draw(_dc, _camera, lights);

            var view = _viewCam.View;
            var proj = _viewCam.Proj;


            var fovX = _viewCam.FovX;
            var fovY = _viewCam.FovY;
            var org = new Vector3();
            var dirs = new[] {
                new Vector3(fovX, fovY, 1.0f),
                new Vector3(-fovX, fovY, 1.0f),
                new Vector3(-fovX, -fovY, 1.0f),
                new Vector3(fovX, -fovY, 1.0f)
            };
            var points = new Vector3[5];

            var edgePlanes = new[] {
                new Plane(1, 0, 0, -_terrain.Width / 2),
                new Plane(-1, 0, 0, _terrain.Width / 2),
                new Plane(0, 1, 0, -_terrain.Depth / 2),
                new Plane(0, -1, 0, _terrain.Depth / 2)
            };

            var invView = Matrix.Invert(view);
            org = Vector3.TransformCoordinate(org, invView);

            var groundPlane = new Plane(new Vector3(), new Vector3(0, 1, 0));
            var ok = true;
            for (int i = 0; i < 4 && ok; i++) {
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
                    foreach (var edgePlane in edgePlanes) {
                        if (Plane.Intersects(edgePlane, org, dirs[i], out hit)) {
                            d = n.Normal.X * hit.X + n.Normal.Y * hit.Y + n.Normal.Z * hit.Z + n.D;
                            if (d >= 0.0f) {
                                hit *= 2;
                                ok = true;
                                break;
                            }
                        }
                    }
                }


                //if (ok) {
                points[i] = new Vector3(Math.Min(Math.Max(hit.X, -float.MaxValue), float.MaxValue), 0, Math.Min(Math.Max(hit.Z, -float.MaxValue), float.MaxValue));
                //}
            }



            
            if (ok) {
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
                for (int i = 0; i < Effects.ColorFX.ColorTech.Description.PassCount; i++) {
                    Effects.ColorFX.SetWorldViewProj(_camera.ViewProj);
                    Effects.ColorFX.ColorTech.GetPassByIndex(i).Apply(_dc);
                    _dc.Draw(5, 0);
                }
            }



        }
    }
}