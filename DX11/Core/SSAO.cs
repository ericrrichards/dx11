using System.Collections.Generic;
using System.Drawing;

using Core.Camera;
using Core.FX;
using Core.Vertex;
using SlimDX;
using SlimDX.Direct3D11;
using SlimDX.DXGI;
using Buffer = SlimDX.Direct3D11.Buffer;
using Device = SlimDX.Direct3D11.Device;
using Format = SlimDX.DXGI.Format;
using MapFlags = SlimDX.Direct3D11.MapFlags;
using Viewport = SlimDX.Direct3D11.Viewport;

namespace Core {
    public class Ssao : DisposableClass {
        #region private members
        private readonly Device _device;
        private readonly DeviceContext _dc;

        // full screen quad used for computing SSAO from normal/depth map
        private Buffer _screenQuadVB;
        private Buffer _screenQuadIB;

        // texture containing random vectors for use in random sampling
        private ShaderResourceView _randomVectorSRV;

        // scene normal/depth texture views
        private RenderTargetView _normalDepthRTV;
        private ShaderResourceView _normalDepthSRV;

        // occlusions maps
        // need two, because we will ping-pong them to do a bilinear blur
        private RenderTargetView _ambientRTV0;
        private ShaderResourceView _ambientSRV0;
        private RenderTargetView _ambientRTV1;
        private ShaderResourceView _ambientSRV1;

        // dimensions of the full screen quad
        private int _renderTargetWidth;
        private int _renderTargetHeight;

        // far-plane corners of the view frustum, used to reconstruct position from depth
        private readonly Vector4[] _frustumFarCorners = new Vector4[4];
        // uniformly distributed vectors that will be randomized to sample occlusion
        readonly Vector4[] _offsets = new Vector4[14];
        // half-size viewport for rendering the occlusion maps
        private Viewport _ambientMapViewport;

        private bool _disposed;
        #endregion
        #region public methods
        // readonly properties to access the normal/depth and occlusion maps
        public ShaderResourceView NormalDepthSRV { get { return _normalDepthSRV; } }
        public ShaderResourceView AmbientSRV { get { return _ambientSRV0; } }

        public Ssao(Device device, DeviceContext dc, int width, int height, float fovY, float farZ) {
            _device = device;
            _dc = dc;
            OnSize(width, height, fovY, farZ);

            BuildFullScreenQuad();
            BuildOffsetVectors();
            BuildRandomVectorTexture();
        }

        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Util.ReleaseCom(ref _screenQuadVB);
                    Util.ReleaseCom(ref _screenQuadIB);
                    Util.ReleaseCom(ref _randomVectorSRV);

                    ReleaseTextureViews();
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        public void OnSize(int width, int height, float fovY, float farZ) {
            _renderTargetWidth = width;
            _renderTargetHeight = height;

            _ambientMapViewport = new Viewport(0, 0, width / 2.0f, height / 2.0f, 0, 1);

            BuildFrustumFarCorners(fovY, farZ);
            BuildTextureViews();
        }

        public void SetNormalDepthRenderTarget(DepthStencilView dsv) {
            _dc.OutputMerger.SetTargets(dsv, _normalDepthRTV);
            var clearColor = new Color4(1e5f, 0.0f, 0.0f, -1.0f);
            _dc.ClearRenderTargetView(_normalDepthRTV, clearColor);
        }
        public void ComputeSsao(CameraBase camera) {
            _dc.OutputMerger.SetTargets((DepthStencilView)null, _ambientRTV0);
            _dc.ClearRenderTargetView(_ambientRTV0, Color.Black);
            _dc.Rasterizer.SetViewports(_ambientMapViewport);

            var T = Matrix.Identity;
            T.M11 = 0.5f;
            T.M22 = -0.5f;
            T.M41 = 0.5f;
            T.M42 = 0.5f;

            var P = camera.Proj;
            var pt = P * T;

            Effects.SsaoFX.SetViewToTexSpace(pt);
            Effects.SsaoFX.SetOffsetVectors(_offsets);
            Effects.SsaoFX.SetFrustumCorners(_frustumFarCorners);
            Effects.SsaoFX.SetNormalDepthMap(_normalDepthSRV);
            Effects.SsaoFX.SetRandomVecMap(_randomVectorSRV);

            var stride = Basic32.Stride;
            const int Offset = 0;

            _dc.InputAssembler.InputLayout = InputLayouts.Basic32;
            _dc.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            _dc.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_screenQuadVB, stride, Offset));
            _dc.InputAssembler.SetIndexBuffer(_screenQuadIB, Format.R16_UInt, 0);

            var tech = Effects.SsaoFX.SsaoTech;
            for (var p = 0; p < tech.Description.PassCount; p++) {
                tech.GetPassByIndex(p).Apply(_dc);
                _dc.DrawIndexed(6, 0, 0);
            }
        }

        public void BlurAmbientMap(int blurCount) {
            for (int i = 0; i < blurCount; i++) {
                BlurAmbientMap(_ambientSRV0, _ambientRTV1, true);
                BlurAmbientMap(_ambientSRV1, _ambientRTV0, false);
            }
        }
        #endregion
        #region private methods

        private void BlurAmbientMap(ShaderResourceView inputSRV, RenderTargetView outputRTV, bool horzBlur) {
            _dc.OutputMerger.SetTargets(outputRTV);
            _dc.ClearRenderTargetView(outputRTV, Color.Black);
            _dc.Rasterizer.SetViewports(_ambientMapViewport);

            Effects.SsaoBlurFX.SetTexelWidth(1.0f / _ambientMapViewport.Width);
            Effects.SsaoBlurFX.SetTexelHeight(1.0f / _ambientMapViewport.Height);
            Effects.SsaoBlurFX.SetNormalDepthMap(_normalDepthSRV);
            Effects.SsaoBlurFX.SetInputImage(inputSRV);

            var tech = horzBlur ? Effects.SsaoBlurFX.HorzBlurTech : Effects.SsaoBlurFX.VertBlurTech;

            var stride = Basic32.Stride;
            const int Offset = 0;

            _dc.InputAssembler.InputLayout = InputLayouts.Basic32;
            _dc.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            _dc.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_screenQuadVB, stride, Offset));
            _dc.InputAssembler.SetIndexBuffer(_screenQuadIB, Format.R16_UInt, 0);

            for (var p = 0; p < tech.Description.PassCount; p++) {
                var pass = tech.GetPassByIndex(p);
                pass.Apply(_dc);
                _dc.DrawIndexed(6, 0, 0);
                Effects.SsaoBlurFX.SetInputImage(null);
                pass.Apply(_dc);
            }
        }

        private void BuildFrustumFarCorners(float fovy, float farz) {
            var aspect = (float)_renderTargetWidth / _renderTargetHeight;

            var halfHeight = farz * MathF.Tan(0.5f * fovy);
            var halfWidth = aspect * halfHeight;

            _frustumFarCorners[0] = new Vector4(-halfWidth, -halfHeight, farz, 0);
            _frustumFarCorners[1] = new Vector4(-halfWidth, +halfHeight, farz, 0);
            _frustumFarCorners[2] = new Vector4(+halfWidth, +halfHeight, farz, 0);
            _frustumFarCorners[3] = new Vector4(+halfWidth, -halfHeight, farz, 0);
        }

        private void BuildFullScreenQuad() {
            var v = new Basic32[4];
            // normal.X contains frustum corner array index
            v[0] = new Basic32(new Vector3(-1, -1, 0), new Vector3(0, 0, 0), new Vector2(0, 1));
            v[1] = new Basic32(new Vector3(-1, +1, 0), new Vector3(1, 0, 0), new Vector2(0, 0));
            v[2] = new Basic32(new Vector3(+1, +1, 0), new Vector3(2, 0, 0), new Vector2(1, 0));
            v[3] = new Basic32(new Vector3(+1, -1, 0), new Vector3(3, 0, 0), new Vector2(1, 1));

            var vbd = new BufferDescription(Basic32.Stride * 4, ResourceUsage.Immutable, BindFlags.VertexBuffer,
                CpuAccessFlags.None, ResourceOptionFlags.None, 0);

            _screenQuadVB = new Buffer(_device, new DataStream(v, false, false), vbd);

            var indices = new short[] { 0, 1, 2, 0, 2, 3 };
            var ibd = new BufferDescription(sizeof(short) * 6, ResourceUsage.Immutable, BindFlags.IndexBuffer,
                CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            _screenQuadIB = new Buffer(_device, new DataStream(indices, false, false), ibd);

        }

        private void BuildTextureViews() {
            ReleaseTextureViews();

            var texDesc = new Texture2DDescription {
                Width = _renderTargetWidth,
                Height = _renderTargetHeight,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.R32G32B32A32_Float,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };
            var normalDepthTex = new Texture2D(_device, texDesc) { DebugName = "normalDepthTex" };
            _normalDepthSRV = new ShaderResourceView(_device, normalDepthTex);
            _normalDepthRTV = new RenderTargetView(_device, normalDepthTex);

            Util.ReleaseCom(ref normalDepthTex);

            texDesc.Width = _renderTargetWidth / 2;
            texDesc.Height = _renderTargetHeight / 2;
            texDesc.Format = Format.R16_Float;

            var ambientTex0 = new Texture2D(_device, texDesc) { DebugName = "ambientTex0" };
            _ambientSRV0 = new ShaderResourceView(_device, ambientTex0);
            _ambientRTV0 = new RenderTargetView(_device, ambientTex0);


            var ambientTex1 = new Texture2D(_device, texDesc) { DebugName = "ambientTex1" };
            _ambientSRV1 = new ShaderResourceView(_device, ambientTex1);
            _ambientRTV1 = new RenderTargetView(_device, ambientTex1);

            Util.ReleaseCom(ref ambientTex0);
            Util.ReleaseCom(ref ambientTex1);

        }

        private void ReleaseTextureViews() {
            Util.ReleaseCom(ref _normalDepthRTV);
            Util.ReleaseCom(ref _normalDepthSRV);

            Util.ReleaseCom(ref _ambientRTV0);
            Util.ReleaseCom(ref _ambientSRV0);

            Util.ReleaseCom(ref _ambientRTV1);
            Util.ReleaseCom(ref _ambientSRV1);
        }

        private void BuildRandomVectorTexture() {
            var texDesc = new Texture2DDescription {
                Width = 256,
                Height = 256,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.R8G8B8A8_UNorm,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None
            };
            var color = new List<Color4>();
            for (var i = 0; i < 256; i++) {
                for (var j = 0; j < 256; j++) {
                    var v = new Vector3(MathF.Rand(0, 1), MathF.Rand(0, 1), MathF.Rand(0, 1));
                    color.Add(new Color4(0, v.X, v.Y, v.Z));
                }
            }
            var tex = new Texture2D(_device, texDesc);

            var box = _dc.MapSubresource(tex, 0, MapMode.WriteDiscard, MapFlags.None);
            foreach (var color4 in color) {
                box.Data.Write((byte)(color4.Red * 255));
                box.Data.Write((byte)(color4.Green * 255));
                box.Data.Write((byte)(color4.Blue * 255));
                box.Data.Write((byte)(0));
            }
            _dc.UnmapSubresource(tex, 0);

            tex.DebugName = "random ssao texture";

            _randomVectorSRV = new ShaderResourceView(_device, tex);

            Util.ReleaseCom(ref tex);
        }

        private void BuildOffsetVectors() {
            // cube corners
            _offsets[0] = new Vector4(+1, +1, +1, 0);
            _offsets[1] = new Vector4(-1, -1, -1, 0);

            _offsets[2] = new Vector4(-1, +1, +1, 0);
            _offsets[3] = new Vector4(+1, -1, -1, 0);

            _offsets[4] = new Vector4(+1, +1, -1, 0);
            _offsets[5] = new Vector4(-1, -1, +1, 0);

            _offsets[6] = new Vector4(-1, +1, -1, 0);
            _offsets[7] = new Vector4(+1, -1, +1, 0);

            // cube face centers
            _offsets[8] = new Vector4(-1, 0, 0, 0);
            _offsets[9] = new Vector4(+1, 0, 0, 0);

            _offsets[10] = new Vector4(0, -1, 0, 0);
            _offsets[11] = new Vector4(0, +1, 0, 0);

            _offsets[12] = new Vector4(0, 0, -1, 0);
            _offsets[13] = new Vector4(0, 0, +1, 0);

            for (var i = 0; i < 14; i++) {
                var s = MathF.Rand(0.25f, 1.0f);
                var v = s * Vector4.Normalize(_offsets[i]);
                _offsets[i] = v;
            }
        }

        #endregion
    }
}
