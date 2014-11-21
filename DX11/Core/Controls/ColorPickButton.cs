using System;
using System.Drawing;
using System.Windows.Forms;
using SlimDX;

namespace Core.Controls {
    public delegate void OnUpdateColorEvent(object sender, Vector3 v);

    public sealed class ColorPickButton : Button {
        private static readonly ColorDialog ColorDialog = new ColorDialog() {
            AllowFullOpen = true,
            AnyColor = true,
            FullOpen = true,
        };
        private Vector3 _color;
        public event OnUpdateColorEvent OnUpdateColor;

        private void OnOnUpdateColor() {
            var handler = OnUpdateColor;
            if (handler != null) handler(this, _color);
        }

        public ColorPickButton(Vector3 color) {
            FlatStyle = FlatStyle.Flat;
            BackColor = ColorFromVector3(color);
            FlatAppearance.BorderColor = Color.Black;

            Click += OnClick;
        }

        private void OnClick(object sender, EventArgs eventArgs) {
            ColorDialog.Color = BackColor;
            if (ColorDialog.ShowDialog() == DialogResult.OK) {
                BackColor = ColorDialog.Color;
                _color = Vector3FromColor(ColorDialog.Color);
                OnOnUpdateColor();
            }
        }

        private static Color ColorFromVector3(Vector3 colorVector) {
            return Color.FromArgb((int)(colorVector.X * 256), (int)(colorVector.Y * 256), (int)(colorVector.Z * 256));
        }

        private static Vector3 Vector3FromColor(Color c) {
            return new Vector3(c.R / 256.0f, c.G / 256.0f, c.B / 256.0f);
        }


    }
    
}