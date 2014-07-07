using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
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

            var _rand = new Random((int)nudSeed.Value);
            var w = splitPanel.Panel2.ClientSize.Width;
            var h = splitPanel.Panel2.ClientSize.Height;

            var numSites = (int)nudNumRegions.Value;
            var sites = new List<Point>();
            var wBuffer = 10;
            var hBuffer = 10;
            for (int i = 0; i < numSites; i++) {
                var p = new Point(_rand.Next(w - wBuffer * 2) + wBuffer, _rand.Next(h - hBuffer * 2) + hBuffer);
                sites.Add(p);
            }

            _graph = Voronoi.ComputeVoronoi(sites, w, h);
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
                var f = new Font(FontFamily.GenericMonospace, 8);
                if (chkShowVertices.Checked) {
                    var vertPen = new SolidBrush(Color.Firebrick);
                    var newVertPen = new SolidBrush(Color.Red);
                    foreach (var vertex in _graph.Vertices) {
                        var r = vertex.New ? new RectangleF(vertex.X - 4, vertex.Y - 4, 8, 8) : new RectangleF(vertex.X - 2, vertex.Y - 2, 4, 4);

                        g.FillEllipse(vertex.New ? newVertPen : vertPen, r);

                        //g.DrawString("#"+vertex.SiteNum, f, newVertPen, r.Right, r.Top);
                    }
                }
                if (chkShowSites.Checked) {
                    var siteBrush = new SolidBrush(Color.Blue);
                    var newSiteBrush = new SolidBrush(Color.LightSkyBlue);
                    foreach (var point in _graph.Sites) {
                        var r = point.New ? new RectangleF(point.X - 4, point.Y - 4, 8, 8) : new RectangleF(point.X - 2, point.Y - 2, 4, 4);
                        g.FillEllipse(point.New ? newSiteBrush : siteBrush, r);
                        //g.DrawString("#" + point.SiteNum, f, newSiteBrush, r.Right, r.Top);
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

            var _rand = new Random((int)nudSeed.Value);
            var myRand = new MyRandom((int) nudSeed.Value);
            

            var w = splitPanel.Panel2.ClientSize.Width;
            var h = splitPanel.Panel2.ClientSize.Height;

            var numSites = (int)nudNumRegions.Value;
            var sites = new List<Point>();
            var wBuffer = 10;
            var hBuffer = 10;
            for (int i = 0; i < numSites; i++) {
                var p = new Point(_rand.Next(w - wBuffer * 2) + wBuffer, _rand.Next(h - hBuffer * 2) + hBuffer);
                sites.Add(p);
            }

            _voronoi = new Voronoi(sites, w, h);
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

    class MyRandom {
        private const int MBIG = Int32.MaxValue;
        private const int MSEED = 161803398;
        private const int MZ = 0;


        //
        // Member Variables 
        // 
        private int inext;
        private int inextp;
        internal int[] SeedArray = new int[56];

        public MyRandom(int Seed) {
            int ii;
            int mj, mk;

            //Initialize our Seed array.
            //This algorithm comes from Numerical Recipes in C (2nd Ed.) 
            int subtraction = (Seed == Int32.MinValue) ? Int32.MaxValue : Math.Abs(Seed);
            mj = MSEED - subtraction;
            SeedArray[55] = mj;
            mk = 1;
            for (int i = 1; i < 55; i++) {  //Apparently the range [1..55] is special (Knuth) and so we're wasting the 0'th position.
                ii = (21 * i) % 55;
                SeedArray[ii] = mk;
                mk = mj - mk;
                if (mk < 0) mk += MBIG;
                mj = SeedArray[ii];
            }
            for (int k = 1; k < 5; k++) {
                for (int i = 1; i < 56; i++) {
                    SeedArray[i] -= SeedArray[1 + (i + 30) % 55];
                    if (SeedArray[i] < 0) SeedArray[i] += MBIG;
                }
            }
            inext = 0;
            inextp = 21;
            Seed = 1;
        }
        protected virtual double Sample() {
            //Including this division at the end gives us significantly improved 
            //random number distribution.
            return (InternalSample() * (1.0 / MBIG));
        }

        private int InternalSample() {
            int retVal;
            int locINext = inext;
            int locINextp = inextp;

            if (++locINext >= 56) locINext = 1;
            if (++locINextp >= 56) locINextp = 1;

            retVal = SeedArray[locINext] - SeedArray[locINextp];

            if (retVal == MBIG) retVal--;
            if (retVal < 0) retVal += MBIG;

            SeedArray[locINext] = retVal;

            inext = locINext;
            inextp = locINextp;

            return retVal;
        }
        public virtual int Next(int maxValue) {
            if (maxValue < 0) {
                throw new ArgumentOutOfRangeException("maxValue", "ArgumentOutOfRange_MustBePositive", "maxValue");
            }
            return (int)(Sample() * maxValue);
        }

    }
}
