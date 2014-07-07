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
        // Drawing stuff
        private readonly Pen _sweepPen;
        private readonly Pen _circlePen;
        private readonly Pen _newCirclePen;
        private readonly Pen _edgePen;
        private readonly Pen _newEdgePen;
        private readonly SolidBrush _vertBrush;
        private readonly SolidBrush _newVertBrush;
        private readonly SolidBrush _siteBrush;
        private readonly SolidBrush _newSiteBrush;

        public MainForm() {
            InitializeComponent();
            _sweepPen = new Pen(Color.Magenta);
            _circlePen = new Pen(Color.Orange);
            _newCirclePen = new Pen(Color.Gold) { Width = 2 };
            _edgePen = new Pen(Color.LightSteelBlue);
            _newEdgePen = new Pen(Color.White) { Width = 2 };
            _vertBrush = new SolidBrush(Color.Firebrick);
            _newVertBrush = new SolidBrush(Color.Red);
            _siteBrush = new SolidBrush(Color.Blue);
            _newSiteBrush = new SolidBrush(Color.LightSkyBlue);
        }

        private void MainForm_Load(object sender, EventArgs e) {
            //GenerateGraph();
            InitializeVoronoi();
            
        }
        private void MainForm_FormClosed(object sender, FormClosedEventArgs e) {
            _circlePen.Dispose();
            _edgePen.Dispose();
            _newCirclePen.Dispose();
            _newEdgePen.Dispose();
            _newSiteBrush.Dispose();
            _newVertBrush.Dispose();
            _vertBrush.Dispose();
            _siteBrush.Dispose();
            _sweepPen.Dispose();
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

                g.DrawLine(_sweepPen, splitPanel.Panel2.ClientRectangle.Left, _graph.SweepLine, splitPanel.Panel2.ClientRectangle.Right, _graph.SweepLine);

                if (chkShowCircles.Checked) {
                    foreach (var triangle in _graph.Triangles) {
                        var circle = new Circle(triangle.V1, triangle.V2, triangle.V3);
                        g.DrawEllipse(triangle.New ? _newCirclePen : _circlePen, circle.GetRect());
                    }
                }
                if (chkShowEdges.Checked) {
                    foreach (var segment in _graph.Segments) {
                        var start = segment.P1;
                        var end = segment.P2;
                        g.DrawLine(segment.New ? _newEdgePen : _edgePen, start, end);
                    }
                }
                if (chkShowVertices.Checked) {
                    foreach (var vertex in _graph.Vertices) {
                        var r = vertex.New ? new RectangleF(vertex.X - 4, vertex.Y - 4, 8, 8) : new RectangleF(vertex.X - 2, vertex.Y - 2, 4, 4);

                        g.FillEllipse(vertex.New ? _newVertBrush : _vertBrush, r);

                    }
                }
                if (chkShowSites.Checked) {
                    foreach (var point in _graph.Sites) {
                        var r = point.New ? new RectangleF(point.X - 4, point.Y - 4, 8, 8) : new RectangleF(point.X - 2, point.Y - 2, 4, 4);
                        g.FillEllipse(point.New ? _newSiteBrush : _siteBrush, r);
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
            Animate((int) nudStepTo.Value);
        }

        private void Animate(int toStep = int.MaxValue) {
            Cursor = Cursors.WaitCursor;
            var lastStep = _voronoi.StepNumber;
            using (var graphics = splitPanel.Panel2.CreateGraphics()) {
                while (_voronoi.StepNumber < toStep) {
                    _voronoi.StepVoronoi();
                    //splitPanel.Panel2.Invalidate();

                    splitContainer1_Panel2_Paint(this, new PaintEventArgs(graphics, splitPanel.Panel2.ClientRectangle));

                    Thread.Sleep(100);
                    if (lastStep == _voronoi.StepNumber) {
                        break;
                    }
                    lastStep = _voronoi.StepNumber;
                }
            }
            Cursor = Cursors.Default;
        }

        private void btnAnimate_Click(object sender, EventArgs e) {
            InitializeVoronoi();
            Animate();
        }

        
    }
}
