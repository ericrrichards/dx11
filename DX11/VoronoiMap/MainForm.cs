using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Windows.Forms;

namespace VoronoiMap {
    public partial class MainForm : Form {
        private VoronoiGraph _graph;
        private Voronoi _voronoi;

        public MainForm() {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e) {
            //GenerateGraph();
            InitializeVoronoi();
        }

        private void GenerateGraph() {

            var rand = new Random((int)nudSeed.Value);
            var w = splitPanel.Panel2.ClientSize.Width;
            var h = splitPanel.Panel2.ClientSize.Height;

            var numSites = (int)nudNumRegions.Value;
            var sites = new List<Point>();
            for (int i = 0; i < numSites; i++) {
                var p = new Point(rand.Next(w ) , rand.Next(h) );
                sites.Add(p);
            }
            

            _graph = Voronoi.ComputeVoronoi(sites, w, h, chDebug.Checked);
            Console.WriteLine("Voronois done!");
        }

        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e) {
            if (_graph != null) {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                g.Clear(BackColor);

                var sweepPen = new Pen(Color.Magenta);
                g.DrawLine(sweepPen, splitPanel.Panel2.ClientRectangle.Left, _graph.SweepLine, splitPanel.Panel2.ClientRectangle.Right, _graph.SweepLine);

                if (chkShowCircles.Checked) {
                    var circlePen = new Pen(Color.Orange);
                    var newCirclePen = new Pen(Color.Gold) { Width = 2 };

                    foreach (var triangle in _graph.Triangles) {
                        var circle = new Circle(triangle.V1, triangle.V2, triangle.V3);
                        g.DrawEllipse(triangle.New ? newCirclePen : circlePen, circle.GetRect());
                    }
                }
                if (chkShowEdges.Checked) {
                    var edgePen = new Pen(Color.LightSteelBlue);
                    var newEdgePen = new Pen(Color.White) { Width = 2 };

                    foreach (var segment in _graph.Segments) {
                        var start = segment.P1;
                        var end = segment.P2;
                        g.DrawLine(segment.New ? newEdgePen : edgePen, start, end);
                    }
                }
                if (chkShowVertices.Checked) {
                    var vertPen = new SolidBrush(Color.Firebrick);
                    var newVertPen = new SolidBrush(Color.Red);
                    foreach (var vertex in _graph.Vertices) {
                        var r = vertex.New ? new RectangleF(vertex.X - 4, vertex.Y - 4, 8, 8) : new RectangleF(vertex.X - 2, vertex.Y - 2, 4, 4);

                        g.FillEllipse(vertex.New ? newVertPen : vertPen, r);

                    }
                }
                if (chkShowSites.Checked) {
                    var siteBrush = new SolidBrush(Color.Blue);
                    var newSiteBrush = new SolidBrush(Color.LightSkyBlue);
                    foreach (var point in _graph.Sites) {
                        var r = point.New ? new RectangleF(point.X - 4, point.Y - 4, 8, 8) : new RectangleF(point.X - 2, point.Y - 2, 4, 4);
                        g.FillEllipse(point.New ? newSiteBrush : siteBrush, r);
                    }
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

        private void btnStepVoronoi_Click(object sender, EventArgs e) {
            _graph = _voronoi.StepVoronoi();
            nudStepTo.Value++;
            splitPanel.Panel2.Invalidate();
        }

        private void btnInitialize_Click(object sender, EventArgs e) {
            InitializeVoronoi();
            splitPanel.Panel2.Invalidate();
        }

        private void InitializeVoronoi() {
            Console.Clear();
            nudStepTo.Value = 0;
            Edge.EdgeCount = 0;

            var rand = new Random((int)nudSeed.Value);
            

            var w = splitPanel.Panel2.ClientSize.Width;
            var h = splitPanel.Panel2.ClientSize.Height;

            var numSites = (int)nudNumRegions.Value;
            var sites = new List<Point>();
            for (int i = 0; i < numSites; i++) {
                var p = new Point(rand.Next(w ) , rand.Next(h ) );
                sites.Add(p);
            }

            _voronoi = new Voronoi(sites, w, h, chDebug.Checked);
            _graph = _voronoi.Initialize();
        }

        private void btnStepTo_Click(object sender, EventArgs e) {
            Cursor = Cursors.WaitCursor;
            while (_voronoi.StepNumber < nudStepTo.Value) {
                _voronoi.StepVoronoi();
                splitContainer1_Panel2_Paint(this, new PaintEventArgs(splitPanel.Panel2.CreateGraphics(), splitPanel.Panel2.ClientRectangle));
                Thread.Sleep(250);
            }
            Cursor = Cursors.Default;
        }
    }
}
