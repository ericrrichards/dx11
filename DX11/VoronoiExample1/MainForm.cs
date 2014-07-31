using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Algorithms.Voronoi;
using Point = Algorithms.Voronoi.Point;
using Rectangle = Algorithms.Voronoi.Rectangle;

namespace VoronoiExample1 {
    public partial class MainForm : Form {
        private Voronoi _voronoi;
        public MainForm() {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e) {
            var rand = new Random(0);
            var points = new List<Point>();
            var r = ClientRectangle;


            for (int i = 0; i < 100; i++) {
                points.Add(new Point(rand.Next(r.Width), rand.Next(r.Height)));
            }
            var rect = new Rectangle(r.X, r.Y, r.Width, r.Height);
            _voronoi = new Voronoi(points, null, rect);

            Invalidate();
        }

        private void MainForm_Paint(object sender, PaintEventArgs e) {
            var g = e.Graphics;

            g.Clear(Color.Black);

            foreach (var siteCoord in _voronoi.SiteCoords()) {
                g.FillEllipse(Brushes.Red, siteCoord.X-2, siteCoord.Y-2, 4, 4);    
            }

            foreach (var vertex in _voronoi.Vertices) {
                g.FillEllipse(Brushes.White, vertex.X-2, vertex.Y-2, 4, 4);
            }
        }
    }
}
