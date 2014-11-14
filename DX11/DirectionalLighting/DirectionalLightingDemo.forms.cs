using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace DirectionalLighting {
    partial class DirectionalLightDemo {
        #region Forms UI crud
        private Label _lblLower;
        private Label _lblUpper;
        private Label _lblDirectional;
        private Label _lblRed;
        private Label _lblGreen;
        private Label _lblBlue;

        private HScrollBar _hsbLowerR;
        private HScrollBar _hsbLowerG;
        private HScrollBar _hsbLowerB;
        private HScrollBar _hsbUpperR;
        private HScrollBar _hsbUpperG;
        private HScrollBar _hsbUpperB;
        private HScrollBar _hsbDirectionalR;
        private HScrollBar _hsbDirectionalG;
        private HScrollBar _hsbDirectionalB;

        private TableLayoutPanel _tblLayout;


        // ReSharper disable once MethodTooLong
        private void AddUIElements() {
            var toolTip = new ToolTip {
                AutoPopDelay = 5000,
                InitialDelay = 1000,
                ReshowDelay = 500,
                ShowAlways = true
            };

            _hsbLowerR = new HScrollBar {
                Minimum = 0,
                Maximum = 109,
                LargeChange = 10,
                SmallChange = 1,
                Value = (int)(_ambientLowerColor.X * 100),
                Dock = DockStyle.Fill
            };
            _hsbLowerR.ValueChanged += (sender, args) => {
                _ambientLowerColor.X = _hsbLowerR.Value / 100.0f;
                toolTip.SetToolTip(_hsbLowerR, _ambientLowerColor.X.ToString(CultureInfo.InvariantCulture));
            };
            _hsbLowerG = new HScrollBar {
                Minimum = 0,
                Maximum = 109,
                LargeChange = 10,
                SmallChange = 1,
                Value = (int)(_ambientLowerColor.Y * 100),
                Dock = DockStyle.Fill
            };
            _hsbLowerG.ValueChanged += (sender, args) => {
                _ambientLowerColor.Y = _hsbLowerG.Value / 100.0f;
                toolTip.SetToolTip(_hsbLowerG, _ambientLowerColor.Y.ToString(CultureInfo.InvariantCulture));
            };
            _hsbLowerB = new HScrollBar {
                Minimum = 0,
                Maximum = 109,
                LargeChange = 10,
                SmallChange = 1,
                Value = (int)(_ambientLowerColor.Z * 100),
                Dock = DockStyle.Fill
            };
            _hsbLowerB.ValueChanged += (sender, args) => {
                _ambientLowerColor.Z = _hsbLowerB.Value / 100.0f;
                toolTip.SetToolTip(_hsbLowerB, _ambientLowerColor.Z.ToString(CultureInfo.InvariantCulture));
            };
            _hsbUpperR = new HScrollBar {
                Minimum = 0,
                Maximum = 109,
                LargeChange = 10,
                SmallChange = 1,
                Value = (int)(_ambientUpperColor.X * 100),
                Dock = DockStyle.Fill
            };
            _hsbUpperR.ValueChanged += (sender, args) => {
                _ambientUpperColor.X = _hsbUpperR.Value / 100.0f;
                toolTip.SetToolTip(_hsbUpperR, _ambientUpperColor.X.ToString(CultureInfo.InvariantCulture));
            };
            _hsbUpperG = new HScrollBar {
                Minimum = 0,
                Maximum = 109,
                LargeChange = 10,
                SmallChange = 1,
                Value = (int)(_ambientUpperColor.Y * 100),
                Dock = DockStyle.Fill
            };
            _hsbUpperG.ValueChanged += (sender, args) => {
                _ambientUpperColor.Y = _hsbUpperG.Value / 100.0f;
                toolTip.SetToolTip(_hsbUpperG, _ambientUpperColor.Y.ToString(CultureInfo.InvariantCulture));
            };
            _hsbUpperB = new HScrollBar {
                Minimum = 0,
                Maximum = 109,
                LargeChange = 10,
                SmallChange = 1,
                Value = (int)(_ambientUpperColor.Z * 100),
                Dock = DockStyle.Fill
            };
            _hsbUpperB.ValueChanged += (sender, args) => {
                _ambientUpperColor.Z = _hsbUpperB.Value / 100.0f;
                toolTip.SetToolTip(_hsbUpperB, _ambientUpperColor.Z.ToString(CultureInfo.InvariantCulture));
            };
            _hsbDirectionalR = new HScrollBar {
                Minimum = 0,
                Maximum = 109,
                LargeChange = 10,
                SmallChange = 1,
                Value = (int)(_dirLightColor.X * 100),
                Dock = DockStyle.Fill
            };
            _hsbDirectionalR.ValueChanged += (sender, args) => {
                _dirLightColor.X = _hsbDirectionalR.Value / 100.0f;
                toolTip.SetToolTip(_hsbDirectionalR, _dirLightColor.X.ToString(CultureInfo.InvariantCulture));
            };
            _hsbDirectionalG = new HScrollBar {
                Minimum = 0,
                Maximum = 109,
                LargeChange = 10,
                SmallChange = 1,
                Value = (int)(_dirLightColor.Y * 100),
                Dock = DockStyle.Fill
            };
            _hsbDirectionalG.ValueChanged += (sender, args) => {
                _dirLightColor.Y = _hsbDirectionalG.Value / 100.0f;
                toolTip.SetToolTip(_hsbDirectionalG, _dirLightColor.Y.ToString(CultureInfo.InvariantCulture));
            };
            _hsbDirectionalB = new HScrollBar {
                Minimum = 0,
                Maximum = 109,
                LargeChange = 10,
                SmallChange = 1,
                Value = (int)(_dirLightColor.Z * 100),
                Dock = DockStyle.Fill
            };
            _hsbDirectionalB.ValueChanged += (sender, args) => {
                _dirLightColor.Z = _hsbDirectionalB.Value / 100.0f;
                toolTip.SetToolTip(_hsbDirectionalB, _dirLightColor.Z.ToString(CultureInfo.InvariantCulture));
            };

            _lblUpper = new Label {
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Text = "Upper Color"
            };
            _lblLower = new Label {
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Text = "Lower Color"
            };
            _lblDirectional = new Label {
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Text = "Directional Color"
            };
            _lblRed = new Label {
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Text = "Red",
            };
            _lblGreen = new Label {
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Text = "Green"
            };
            _lblBlue = new Label {
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Text = "Blue"
            };


            _tblLayout = new TableLayoutPanel {
                Dock = DockStyle.Top,
                AutoSize = true
            };
            _tblLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _tblLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _tblLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _tblLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _tblLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent) { Width = .25f });
            _tblLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent) { Width = .25f });
            _tblLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent) { Width = .25f });
            _tblLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent) { Width = .25f });

            _tblLayout.Controls.Add(_lblUpper, 0, 1);
            _tblLayout.Controls.Add(_lblLower, 0, 2);
            _tblLayout.Controls.Add(_lblDirectional, 0, 3);
            _tblLayout.Controls.Add(_lblRed, 1, 0);
            _tblLayout.Controls.Add(_lblGreen, 2, 0);
            _tblLayout.Controls.Add(_lblBlue, 3, 0);

            _tblLayout.Controls.Add(_hsbUpperR, 1, 1);
            _tblLayout.Controls.Add(_hsbUpperG, 2, 1);
            _tblLayout.Controls.Add(_hsbUpperB, 3, 1);
            _tblLayout.Controls.Add(_hsbLowerR, 1, 2);
            _tblLayout.Controls.Add(_hsbLowerG, 2, 2);
            _tblLayout.Controls.Add(_hsbLowerB, 3, 2);
            _tblLayout.Controls.Add(_hsbDirectionalR, 1, 3);
            _tblLayout.Controls.Add(_hsbDirectionalG, 2, 3);
            _tblLayout.Controls.Add(_hsbDirectionalB, 3, 3);

            Window.Controls.Add(_tblLayout);
        }
        #endregion
    }
}
