using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Fortune.FromJS {
    public partial class MainForm : Form {
        private Random _rand = new Random();
        private VoronoiGraph _graph;


        public MainForm() {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e) {
            GenerateGraph();
        }

        private void GenerateGraph() {
            var w = splitPanel.Panel2.ClientSize.Width;
            var h = splitPanel.Panel2.ClientSize.Width;

            var numSites = (int) nudNumRegions.Value;
            var sites = new List<Point>();
            for (int i = 0; i < numSites; i++) {
                var p = new Point(_rand.Next(w), _rand.Next(h));
                sites.Add(p);
            }

            _graph = Voronoi.ComputeVoronoi(sites, w, h);
            Console.WriteLine("Voronois done!");
        }
        
        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e) {
            var g = e.Graphics;

            g.Clear(BackColor);
            if (chkShowSites.Checked) {
                var siteBrush = new SolidBrush(Color.Blue);
                foreach (var point in _graph.Sites) {
                    var r = new RectangleF(point.X - 2, point.Y - 2, 4, 4);
                    g.FillEllipse(siteBrush, r);
                }
            }
            if (chkShowVertices.Checked) {
                var vertPen = new Pen(Color.Red);
                foreach (var vertex in _graph.Vertices) {
                    var r = new RectangleF(vertex.X - 4, vertex.Y - 4, 8, 8);

                    g.DrawEllipse(vertPen, r);

                }
            }
            if (chkShowEdges.Checked) {
                var edgePen = new Pen(Color.White);

                foreach (var segment in _graph.Segments) {
                    var start = segment.P1;
                    var end = segment.P2;
                    g.DrawLine(edgePen, start, end);
                }
            }
        }

        private void btnRegen_Click(object sender, EventArgs e) {
            GenerateGraph();
            splitPanel.Panel2.Invalidate();
        }

        private void chkShowEdges_CheckedChanged(object sender, EventArgs e) {
            splitPanel.Panel2.Invalidate();
        }
    }
}
