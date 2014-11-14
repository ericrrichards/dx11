using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace HemisphericalAmbient {
    partial class HemisphericalAmbientDemo {
        #region Forms UI crud
        private Label _lblLower;
        private Label _lblUpper;
        private Label _lblRed;
        private Label _lblGreen;
        private Label _lblBlue;

        private HScrollBar _hsbLowerR;
        private HScrollBar _hsbLowerG;
        private HScrollBar _hsbLowerB;
        private HScrollBar _hsbUpperR;
        private HScrollBar _hsbUpperG;
        private HScrollBar _hsbUpperB;

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
                Value = (int)(_ambientLower.X * 100),
                Dock = DockStyle.Fill
            };
            _hsbLowerR.ValueChanged += (sender, args) => {
                _ambientLower.X = _hsbLowerR.Value / 100.0f;
                toolTip.SetToolTip(_hsbLowerR, _ambientLower.X.ToString(CultureInfo.InvariantCulture));
            };
            _hsbLowerG = new HScrollBar {
                Minimum = 0,
                Maximum = 109,
                LargeChange = 10,
                SmallChange = 1,
                Value = (int)(_ambientLower.Y * 100),
                Dock = DockStyle.Fill
            };
            _hsbLowerG.ValueChanged += (sender, args) => {
                _ambientLower.Y = _hsbLowerG.Value / 100.0f;
                toolTip.SetToolTip(_hsbLowerG, _ambientLower.Y.ToString(CultureInfo.InvariantCulture));
            };
            _hsbLowerB = new HScrollBar {
                Minimum = 0,
                Maximum = 109,
                LargeChange = 10,
                SmallChange = 1,
                Value = (int)(_ambientLower.Z * 100),
                Dock = DockStyle.Fill
            };
            _hsbLowerB.ValueChanged += (sender, args) => {
                _ambientLower.Z = _hsbLowerB.Value / 100.0f;
                toolTip.SetToolTip(_hsbLowerB, _ambientLower.Z.ToString(CultureInfo.InvariantCulture));
            };
            _hsbUpperR = new HScrollBar {
                Minimum = 0,
                Maximum = 109,
                LargeChange = 10,
                SmallChange = 1,
                Value = (int)(_ambientUpper.X * 100),
                Dock = DockStyle.Fill
            };
            _hsbUpperR.ValueChanged += (sender, args) => {
                _ambientUpper.X = _hsbUpperR.Value / 100.0f;
                toolTip.SetToolTip(_hsbUpperR, _ambientUpper.X.ToString(CultureInfo.InvariantCulture));
            };
            _hsbUpperG = new HScrollBar {
                Minimum = 0,
                Maximum = 109,
                LargeChange = 10,
                SmallChange = 1,
                Value = (int)(_ambientUpper.Y * 100),
                Dock = DockStyle.Fill
            };
            _hsbUpperG.ValueChanged += (sender, args) => {
                _ambientUpper.Y = _hsbUpperG.Value / 100.0f;
                toolTip.SetToolTip(_hsbUpperG, _ambientUpper.Y.ToString(CultureInfo.InvariantCulture));
            };
            _hsbUpperB = new HScrollBar {
                Minimum = 0,
                Maximum = 109,
                LargeChange = 10,
                SmallChange = 1,
                Value = (int)(_ambientUpper.Z * 100),
                Dock = DockStyle.Fill
            };
            _hsbUpperB.ValueChanged += (sender, args) => {
                _ambientUpper.Z = _hsbUpperB.Value / 100.0f;
                toolTip.SetToolTip(_hsbUpperB, _ambientUpper.Z.ToString(CultureInfo.InvariantCulture));
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
            _tblLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent) { Width = .25f });
            _tblLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent) { Width = .25f });
            _tblLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent) { Width = .25f });
            _tblLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent) { Width = .25f });

            _tblLayout.Controls.Add(_lblUpper, 0, 1);
            _tblLayout.Controls.Add(_lblLower, 0, 2);
            _tblLayout.Controls.Add(_lblRed, 1, 0);
            _tblLayout.Controls.Add(_lblGreen, 2, 0);
            _tblLayout.Controls.Add(_lblBlue, 3, 0);

            _tblLayout.Controls.Add(_hsbUpperR, 1, 1);
            _tblLayout.Controls.Add(_hsbUpperG, 2, 1);
            _tblLayout.Controls.Add(_hsbUpperB, 3, 1);
            _tblLayout.Controls.Add(_hsbLowerR, 1, 2);
            _tblLayout.Controls.Add(_hsbLowerG, 2, 2);
            _tblLayout.Controls.Add(_hsbLowerB, 3, 2);


            Window.Controls.Add(_tblLayout);
        }
        #endregion
    }
}
