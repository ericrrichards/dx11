using System.Drawing;
using System.Windows.Forms;
using Core.Controls;

namespace PointLighting {
    partial class PointLightDemo {
        private Label _lblLower;
        private Label _lblUpper;
        private Label _lblDirectional;
        private Label _lblColor;
        private Label _lblRange;

        private TableLayoutPanel _tblLayout;


        private ColorPickButton _btnLowerAmbientColor;
        private ColorPickButton _btnUpperAmbientColor;
        private ColorPickButton _btnDirLightColor;
        private HScrollBar _hsbLightRange;
        private void AddUIElements() {
            _lblUpper = new Label {
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Text = "Ambient Upper Color"
            };
            _lblLower = new Label {
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Text = "Ambient Lower Color"
            };
            _lblDirectional = new Label {
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Text = "Directional Color"
            };
            _lblColor = new Label {
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Text = "Color",
            };
            _lblRange = new Label {
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Text = "Light Range",
            };


            _tblLayout = new TableLayoutPanel {
                AutoSize = true,
            };
            _tblLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _tblLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _tblLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _tblLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _tblLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _tblLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _tblLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            _tblLayout.Controls.Add(_lblUpper, 0, 1);
            _tblLayout.Controls.Add(_lblLower, 0, 2);
            _tblLayout.Controls.Add(_lblDirectional, 0, 3);
            _tblLayout.Controls.Add(_lblRange, 0, 4);
            _tblLayout.Controls.Add(_lblColor, 1, 0);




            _btnLowerAmbientColor = new ColorPickButton(_ambientLowerColor);
            _btnLowerAmbientColor.OnUpdateColor += (sender, vector3) => _ambientLowerColor = vector3;

            _btnUpperAmbientColor = new ColorPickButton(_ambientUpperColor);
            _btnUpperAmbientColor.OnUpdateColor += (sender, vector3) => _ambientUpperColor = vector3;

            _btnDirLightColor = new ColorPickButton(_dirLightColor);
            _btnDirLightColor.OnUpdateColor += (sender, vector3) => _dirLightColor = vector3;

            _hsbLightRange = new HScrollBar {
                Minimum = 0,
                Maximum = 100,
                Value = (int) _lightRange
            };
            _hsbLightRange.Scroll += (sender, args) => {
                _lightRange = _hsbLightRange.Value;
                this.Window.ActiveControl = null;
            };
            _hsbLightRange.ValueChanged += (sender, args) => {
                _lightRange = _hsbLightRange.Value;
                this.Window.ActiveControl = null;
            };


            _tblLayout.Controls.Add(_btnUpperAmbientColor, 1, 1);
            _tblLayout.Controls.Add(_btnLowerAmbientColor, 1, 2);
            _tblLayout.Controls.Add(_btnDirLightColor, 1, 3);
            _tblLayout.Controls.Add(_hsbLightRange, 1, 4);

            Window.Controls.Add(_tblLayout);
        }
    }
}