using System;
using System.Linq;

namespace Core {
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;

    using SlimDX;
    using SlimDX.Direct3D11;

    using Device = SlimDX.Direct3D11.Device;

    public class TextureAtlas : DisposableClass {
        private bool _disposed;
        private readonly Matrix[] _texTransforms;
        private ShaderResourceView _textureView;

        public int Rows { get; private set; }
        public int Columns { get; private set; }
        public int NumCells { get; private set; }

        public ShaderResourceView TextureView {
            get { return _textureView; }
            private set { _textureView = value; }
        }

        public Matrix GetTexTransform(int i) {
            System.Diagnostics.Debug.Assert(i >= 0 && i < NumCells);
            return _texTransforms[i];
        }

        public TextureAtlas(Device device, ICollection<string> filenames) {
            NumCells = filenames.Count;

            // Note: all textures need to be the same size
            var tex = Texture2D.FromFile(device, filenames.First());
            var w = tex.Description.Width;
            var h = tex.Description.Height;
            tex.Dispose();

            Columns = Math.Min(8192 / w, (int)Math.Ceiling(Math.Sqrt(NumCells)));
            Rows = Math.Min(8192 / h, (int)Math.Ceiling((float)NumCells/Columns));

            System.Diagnostics.Debug.Assert(Columns * Rows >= NumCells);

            var bitmap = new Bitmap(Columns * w, Rows * h);
            _texTransforms = new Matrix[NumCells];

            using (var g = Graphics.FromImage(bitmap)) {
                g.Clear(Color.Black);
                var r = 0;
                var c = 0;
                foreach (var filename in filenames) {
                    g.DrawImage(new Bitmap(filename), new Point(c*w, r*h) );

                    _texTransforms[r * Columns + c] =
                        Matrix.Scaling(1.0f/(Columns*2), 1.0f / (2*Rows), 0) * 
                        Matrix.Translation(c * w / (float)bitmap.Width, r * h / (float)bitmap.Width, 0);

                    c++;
                    if (c >= Columns) {
                        c = 0;
                        r++;
                    }

                }
            }
            var tmpFile = Path.GetTempFileName() + ".bmp";
            bitmap.Save(tmpFile);

            TextureView = ShaderResourceView.FromFile(device, tmpFile);
            TextureView.Resource.DebugName = "texture atlas: " +filenames.Aggregate((i, j) => i + ", " + j);
        }
        
        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Util.ReleaseCom(ref _textureView);
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }
    }
}
