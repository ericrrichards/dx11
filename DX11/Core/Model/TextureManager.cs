using System.Collections.Generic;
using System.Linq;
using SlimDX.Direct3D11;

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
    }
}