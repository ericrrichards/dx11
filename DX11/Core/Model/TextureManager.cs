using System.Collections.Generic;
using System.Drawing;
using SlimDX.Direct3D11;
using SlimDX.DXGI;
using Device = SlimDX.Direct3D11.Device;
using Format = SlimDX.DXGI.Format;
using MapFlags = SlimDX.Direct3D11.MapFlags;

namespace Core.Model {
    using System.IO;

    public class TextureManager : DisposableClass {
        private bool _disposed;
        private Device _device;
        private readonly Dictionary<string, ShaderResourceView> _textureSRVs;
        public TextureManager() {
            _textureSRVs = new Dictionary<string, ShaderResourceView>();

            
        }
        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {

                    foreach (var shaderResourceView in _textureSRVs) {
                        var resourceView = shaderResourceView.Value;
                        Util.ReleaseCom(ref resourceView);
                    }
                    _textureSRVs.Clear();
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }
        public void Init(Device device) {
            _device = device;

            Create1By1Tex(device, Color.White, "default");
            Create1By1Tex(device, Color.DeepSkyBlue, "defaultNorm");
        }

        private void Create1By1Tex(Device device, Color color, string texName) {
            var desc2 = new Texture2DDescription {
                SampleDescription = new SampleDescription(1, 0),
                Width = 1,
                Height = 1,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.R8G8B8A8_UNorm,
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.Write
            };
            var texture = new Texture2D(device, desc2);

            var db = device.ImmediateContext.MapSubresource(texture, 0, 0, MapMode.WriteDiscard, MapFlags.None);

            db.Data.Write(color);
            device.ImmediateContext.UnmapSubresource(texture, 0);

            _textureSRVs[texName] = new ShaderResourceView(device, texture);
            Util.ReleaseCom(ref texture);
        }

        public ShaderResourceView CreateTexture(string path) {
            if (!_textureSRVs.ContainsKey(path)) {
                if (File.Exists(path)) {
                    _textureSRVs[path] = ShaderResourceView.FromFile(_device, path);
                    using (var r = _textureSRVs[path].Resource) {
                        r.DebugName = path;
                    }
                } else {
                    return null;
                }
            }
            return _textureSRVs[path];

        }

        public ShaderResourceView this[string tex] {
            get { return _textureSRVs[tex]; }
        }
    }
}