using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace VoronoiMap {
    public sealed partial class MainForm : Form {
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
        private Bitmap _bitmap;

        public MainForm() {
            InitializeComponent();
            _sweepPen = new Pen(Color.Magenta);
            _circlePen = new Pen(Color.Lime);
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
            _bitmap = new Bitmap(splitPanel.Panel2.ClientSize.Width, splitPanel.Panel2.ClientSize.Height);
            cbCircles.SelectedIndex = 0;
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
            _bitmap.Dispose();
        }

        private void GenerateGraph() {

            var rand = new Random((int)nudSeed.Value);
            var w = splitPanel.Panel2.ClientSize.Width;
            var h = splitPanel.Panel2.ClientSize.Height;

            var numSites = (int)nudNumRegions.Value;
            var sites = new List<PointF>();
            for (int i = 0; i < numSites; i++) {
                var p = new Point(rand.Next(w), rand.Next(h));
                sites.Add(p);
            }
            if (nudRelax.Value > 0) {
                sites = RelaxPoints((int)nudRelax.Value, sites);
            }


            _graph = VoronoiGraph.ComputeVoronoi(sites, w, h, chDebug.Checked);
            Console.WriteLine("Voronois done!");
        }

        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e) {
            var g = e.Graphics;
            PaintDiagram(g);
        }

        private void PaintDiagram(Graphics g, bool full=true) {

            using (var g1 = Graphics.FromImage(_bitmap)) {
                g1.SmoothingMode = SmoothingMode.AntiAlias;

                if (full) {
                    PaintDiagramFull(g1);
                } else {
                    PaintDiagramIncremental(g1);
                }

                g.DrawImage(_bitmap, new PointF());
            }
        }

        private void PaintDiagramFull(Graphics g) {
            if (_graph != null) {
                g.Clear(BackColor);

                var item = cbCircles.SelectedIndex;

                var gp = new GraphicsPath();
                var gp2 = new GraphicsPath();
                var gp3 = new GraphicsPath();
                foreach (var point in _graph.Sites) {
                    var r = new RectangleF(point.X - 2, point.Y - 2, 4, 4);
                    if (chkShowSites.Checked) {
                        gp.AddEllipse(r);
                    }

                    foreach (var edge in point.Edges) {
                        var start = edge.RightSite;
                        var end = edge.LeftSite;
                        if (item == 2) {
                            gp2.AddLine(start, end);
                            gp2.CloseFigure();
                        }

                        if (chkShowEdges.Checked) {
                            var visibleClipBounds = g.VisibleClipBounds;
                            
                            var region = point.Region(visibleClipBounds).Where(p=>p!=null).Select(p=>(PointF)p).ToArray();
                            if ( region.Count() >= 3)
                                gp3.AddPolygon(region);
                        }
                    }
                }
                g.DrawPath(_circlePen, gp2);
                g.DrawPath(_edgePen, gp3);
                g.FillPath(_siteBrush, gp);
            }
        }


        private void PaintDiagramIncremental(Graphics g) {
            if (_graph != null) {

                g.Clear(BackColor);

                g.DrawLine(_sweepPen, splitPanel.Panel2.ClientRectangle.Left, _graph.SweepLine, splitPanel.Panel2.ClientRectangle.Right, _graph.SweepLine);

                var item = cbCircles.SelectedIndex;

                if (item == 1) {
                    var gp = new GraphicsPath();
                    foreach (var triangle in _graph.Triangles) {
                        var circle = new Circle(triangle.V1, triangle.V2, triangle.V3);
                        if (triangle.New) {
                            g.DrawEllipse(_newCirclePen, circle.GetRect());
                        } else {
                            gp.AddEllipse(circle.GetRect());
                        }
                    }
                    g.DrawPath(_circlePen, gp);
                } else if (item == 2) {
                    var gp = new GraphicsPath();
                    foreach (var triangle in _graph.Triangles) {
                        if (triangle.New) {
                            g.DrawPolygon(_newCirclePen, new[] { (PointF)triangle.V1, triangle.V2, triangle.V3 });
                        } else {
                            gp.AddPolygon(new[] { (PointF)triangle.V1, triangle.V2, triangle.V3 });
                        }
                    }
                    g.DrawPath(_circlePen, gp);
                }
                
                if (chkShowEdges.Checked) {
                    var gp = new GraphicsPath();
                    
                    foreach (var segment in _graph.Segments) {
                        var start = segment.P1;
                        var end = segment.P2;
                        if (segment.New) {
                            g.DrawLine(_newEdgePen, start, end);
                        } else {
                            gp.AddLine(start, end);
                            gp.CloseFigure();
                        }
                    }
                    g.DrawPath(_edgePen, gp);
                }


                if (chkShowVertices.Checked) {
                    var gp = new GraphicsPath();
                    foreach (var vertex in _graph.Vertices) {
                        var r = vertex.New ? 
                            new RectangleF(vertex.X - 4, vertex.Y - 4, 8, 8)
                            : new RectangleF(vertex.X - 2, vertex.Y - 2, 4, 4)
                            ;
                        if (vertex.New) {
                            g.FillEllipse(_newVertBrush, r);
                        } else {
                            gp.AddEllipse(r);
                        }
                    }
                    g.DrawPath(new Pen(Color.Red), gp);
                }
                if (chkShowSites.Checked) {
                    var gp = new GraphicsPath();
                    foreach (var point in _graph.Sites) {
                        var r = point.New ? new RectangleF(point.X - 4, point.Y - 4, 8, 8) : new RectangleF(point.X - 2, point.Y - 2, 4, 4);
                        if (point.New) {
                            g.FillEllipse(_newSiteBrush, r);
                        } else {
                            gp.AddEllipse(r);
                        }
                    }
                    g.FillPath(_siteBrush, gp);
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
            using (var graphics = splitPanel.Panel2.CreateGraphics()) {
                _graph = _voronoi.StepVoronoi();
                nudStepTo.Value++;
                PaintDiagram(graphics, false);
            }
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
            var sites = new List<PointF>();
            Console.WriteLine(w);
            Console.WriteLine(h);
            for (int i = 0; i < numSites; i++) {
                var p = new Point(rand.Next(w), rand.Next(h));
                sites.Add(p);
            }
            if (nudRelax.Value > 0) {
                sites = RelaxPoints((int) nudRelax.Value, sites);
            }


            _voronoi = new Voronoi(sites, w, h, chDebug.Checked);
            _graph = _voronoi.Initialize();
        }

        private List<PointF> RelaxPoints(int times, List<PointF> points) {
            var ret = new List<PointF>();
            var w = splitPanel.Panel2.ClientSize.Width;
            var h = splitPanel.Panel2.ClientSize.Height;
            var tempPoints = points;
            for (int i = 0; i < times; i++) {
                var voronoi = VoronoiGraph.ComputeVoronoi(tempPoints, w, h);
                tempPoints.Clear();
                foreach (var site in voronoi.Sites) {
                    var region = site.Region(splitPanel.Panel2.ClientRectangle);
                    var p = new PointF(0, 0);
                    foreach (var q in region) {
                        p.X += q.X;
                        p.Y += q.Y;
                    }
                    p.X /= region.Count;
                    p.Y /= region.Count;
                    tempPoints.Add(p);
                }
                ret = tempPoints;
            }
            
            return ret;
        }

        private void btnStepTo_Click(object sender, EventArgs e) {
            Animate((int)nudStepTo.Value);
        }

        private void Animate(int toStep = int.MaxValue) {
            Cursor = Cursors.WaitCursor;
            var lastStep = _voronoi.StepNumber;
            using (var graphics = splitPanel.Panel2.CreateGraphics()) {
                while (_voronoi.StepNumber < toStep) {
                    _voronoi.StepVoronoi();

                    PaintDiagram(graphics, false);

                    Thread.Sleep(10);
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

        private void MainForm_Resize(object sender, EventArgs e) {
            _bitmap.Dispose();
            _bitmap = new Bitmap(splitPanel.Panel2.ClientSize.Width, splitPanel.Panel2.ClientSize.Height);
            btnRegen_Click(null, null);
        }

        private void cbCircles_SelectedIndexChanged(object sender, EventArgs e) {
            splitPanel.Panel2.Invalidate();
        }


    }
}
