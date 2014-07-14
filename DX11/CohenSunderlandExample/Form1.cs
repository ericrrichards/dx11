using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Algorithms;

namespace CohenSutherlandExample {
    public partial class Form1 : Form {
        private const int opacity = 127;

        private List<Tuple<PointF, PointF>> _lines = new List<Tuple<PointF, PointF>>();

        private PointF? _prevPoint;
        private RectangleF _bounds;

        public Form1() {
            InitializeComponent();
        }
        
        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e) {
            var width = splitContainer1.Panel2.ClientSize.Width;
            var height = splitContainer1.Panel2.ClientSize.Height;
            var height3 = height / 3.0f;
            var width3 = width / 3.0f;

            _bounds = new RectangleF(width3, height3, width3, height3);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;



            var left = new RectangleF(0, 0, width3, height);
            var c = Color.FromArgb(opacity, Color.Red);
            g.FillRectangle(new SolidBrush(c), left);

            var top = new RectangleF(0, 0, width, height3);
            c = Color.FromArgb(opacity, Color.Blue);
            g.FillRectangle(new SolidBrush(c), top);

            var right = new RectangleF(2 * width3, 0, width3, height);
            c = Color.FromArgb(opacity, 0, 255, 0);
            g.FillRectangle(new SolidBrush(c), right);

            var bottom = new RectangleF(0, 2 * height3, width, height3);
            c = Color.FromArgb(opacity, Color.Yellow);
            g.FillRectangle(new SolidBrush(c), bottom);
            

            g.DrawLine(Pens.Gray, width3, 0, width3, height);
            g.DrawLine(Pens.Gray, 2 * width3, 0, 2 * width3, height);
            g.DrawLine(Pens.Gray, 0, height3, width, height3);
            g.DrawLine(Pens.Gray, 0, 2 * height3, width, 2 * height3);


            var rects = new[] {
                new RectangleF(0,0, width3, height3),
                new RectangleF(width3, 0, width3, height3),
                new RectangleF(2*width3, 0, width3, height3),
 
                new RectangleF(0,height3, width3, height3),
                new RectangleF(width3, height3, width3, height3),
                new RectangleF(2*width3, height3, width3, height3),

                new RectangleF(0,2*height3, width3, height3),
                new RectangleF(width3, 2*height3, width3, height3),
                new RectangleF(2*width3, 2*height3, width3, height3)
            };
            var codes = new[] {
                "Left|Top", "Top", "Right|Top",
                "Left", "Inside", "Right",
                "Left|Bottom", "Bottom", "Right|Bottom"
            };

            for (int i = 0; i < codes.Length; i++) {
                var brush =  Brushes.Black;
                g.DrawString(codes[i], DefaultFont, brush, rects[i], new StringFormat {Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center});
            }


            foreach (var line in _lines) {
                g.DrawLine(new Pen(Color.Black, 2), line.Item1, line.Item2);
            }
        }

        private void btnClearLines_Click(object sender, EventArgs e) {
            _lines = new List<Tuple<PointF, PointF>>();
            _prevPoint = null;
            splitContainer1.Panel2.Invalidate();
        }

        private void splitContainer1_Panel2_MouseClick(object sender, MouseEventArgs e) {
            var position = e.Location;
            if (_prevPoint == null) {
                _prevPoint = position;
            } else {
                var line = new Tuple<PointF, PointF>(_prevPoint.Value, position);
                _lines.Add(line);
                _prevPoint = null;
                splitContainer1.Panel2.Invalidate();
            }
        }

        private void btnClipLines_Click(object sender, EventArgs e) {
            var newLines = new List<Tuple<PointF, PointF>>(); 
            foreach (var line in _lines) {
                var p1 = line.Item1;
                var p2 = line.Item2;
                var clipped = CohenSutherland.ClipSegment(_bounds, p1, p2);
                if (clipped != null) {
                    newLines.Add(clipped);
                }
            }
            _lines = newLines;
            splitContainer1.Panel2.Invalidate();
        }

        private void Form1_Resize(object sender, EventArgs e) {
            _lines.Clear();
        }
    }
}
