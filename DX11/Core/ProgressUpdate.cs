using System.Drawing;
using System.Threading;
using SlimDX;
using SlimDX.Direct2D;
using SlimDX.DirectWrite;
using Factory = SlimDX.DirectWrite.Factory;
using FactoryType = SlimDX.DirectWrite.FactoryType;
using FontStyle = SlimDX.DirectWrite.FontStyle;

namespace Core {
    public class ProgressUpdate : DisposableClass {
        private bool _disposed;
        private SlimDX.Direct2D.Brush _brush;
        private readonly WindowRenderTarget _rt;
        private Rectangle _barBounds;
        private SlimDX.Direct2D.Brush _clearColor;
        private TextFormat _txtFormat;
        private Rectangle _textRect;
        private Factory _factory;
        private Rectangle _borderBounds;

        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Util.ReleaseCom(ref _brush);
                    Util.ReleaseCom(ref _clearColor);
                    Util.ReleaseCom(ref _txtFormat);
                    Util.ReleaseCom(ref _factory);

                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        public ProgressUpdate(WindowRenderTarget rt1) {
            _rt = rt1;
            _brush = new SolidColorBrush(_rt, Color.Green);
            _clearColor = new SolidColorBrush(_rt, Color.Black);

            _borderBounds = new Rectangle(18, rt1.PixelSize.Height / 2 - 2, rt1.PixelSize.Width - 36, 24);
            _barBounds = new Rectangle(20, rt1.PixelSize.Height / 2, rt1.PixelSize.Width - 40, 20);

            _factory = new Factory(FactoryType.Isolated);
            _txtFormat = new TextFormat(_factory, "Arial", FontWeight.Normal, FontStyle.Normal, FontStretch.Normal, 18, "en-us") {
                TextAlignment = TextAlignment.Center
            };
            _textRect = new Rectangle(100, rt1.PixelSize.Height / 2 - 25, _rt.PixelSize.Width - 200, 20);
        }

        public void Draw(float prc, string msg, bool clear = true) {
            _rt.BeginDraw();

            _rt.Transform = Matrix3x2.Identity;
            if (clear) {
                _rt.Clear(Color.Silver);
            }
            _rt.FillRectangle(_clearColor, _borderBounds);

            var r = new Rectangle(_barBounds.X, _barBounds.Y, (int)(_barBounds.Width * prc), _barBounds.Height);
            _rt.FillRectangle(_brush, r);
            _rt.DrawText(msg, _txtFormat, _textRect, _brush);

            _rt.EndDraw();
        }
    }
}