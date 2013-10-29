using System.Collections.Generic;
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
                    foreach (var key in _textureSRVs.Keys) {
                        var shaderResourceView = _textureSRVs[key];
                        Util.ReleaseCom(ref shaderResourceView);
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
                    _textureSRVs[path].Resource.DebugName = path;
                } else {
                    return null;
                }
            }
            return _textureSRVs[path];

        }
    }
}