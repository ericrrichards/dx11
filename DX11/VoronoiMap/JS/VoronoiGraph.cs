using System;
using System.Collections.Generic;
using System.Drawing;

namespace Fortune.FromJS {
    public class VoronoiGraph {
        public bool Debug { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public readonly List<PointF> Sites = new List<PointF>();
        public readonly List<PointF> Vertices = new List<PointF>();
        public readonly List<Segment> Segments = new List<Segment>(); 
        public readonly List<Triangle> Triangles = new List<Triangle>(); 

        public VoronoiGraph(int width=800, int height=600) {
            Width = width;
            Height = height;
            Debug = false;
        }

        public void PlotSite(Site site) {
            Sites.Add(new PointF(site.X, site.Y));
            if (Debug) {
                Console.WriteLine("site {0},{1}", site.X, site.Y);
            }
        }

        public void PlotBisector(Edge e) {
            if (Debug) {
                Console.WriteLine("bisector {0} {1}", e.Region[Side.Left], e.Region[Side.Right]);
            }
        }

        public void PlotEndpoint(Edge e) {
            if (Debug) {
                Console.WriteLine("EP {0}", e);
            }
            ClipLine(e);
        }

        public void PlotVertex(Site s) {
            if (Debug) {
                Console.WriteLine("vertex {0},{1}", s.X, s.Y);
            }
            Vertices.Add(new PointF(s.X, s.Y));
        }

        private void ClipLine(Edge e) {
            var dx = Height;
            var dy = Width;
            var d = (dx > dy) ? dx : dy;
            var pxMin = -(d - dx)/2;
            var pxMax = Width + (d - dx)/2;
            var pyMin = -(d - dy)/2;
            var pyMax = Height + (d - dy)/2;
            
            Site s1, s2;
            float x1, x2, y1, y2;
            if (Math.Abs(e.A - 1) < Geometry.Tolerance && e.B >= 0) {
                s1 = e.Endpoint[Side.Right];
                s2 = e.Endpoint[Side.Left];
            } else {
                s1 = e.Endpoint[Side.Left];
                s2 = e.Endpoint[Side.Right];
            }

            if (s1 != null && s2 != null) {
                if ((s1.Y < pyMin && s2.Y > pyMax) || (s1.Y > pyMax && s2.Y < pyMin)) {
                    return;
                }
            }




            if (Math.Abs(e.A - 1) < Geometry.Tolerance) {
                y1 = pyMin;
                if (s1 != null && s1.Y > pyMin) {
                    y1 = s1.Y;
                }
                if (y1 > pyMax) {
                    return;
                }
                x1 = e.C - e.B*y1;
                y2 = pyMax;
                if (s2 != null && s2.Y < pyMax) {
                    y2 = s2.Y;
                }
                if (y2 < pyMin) {
                    return;
                }
                x2 = e.C - e.B*y2;
                if (((x1 > pxMax) && (x2 > pxMax)) || ((x1 < pxMin) && (x2 < pxMin))) {
                    return;
                }
                if (x1 > pxMax) {
                    x1 = pxMax;
                    y1 = (e.C - x1)/e.B;
                }
                if (x1 < pxMin) {
                    x1 = pxMin;
                    y1 = (e.C - x1)/e.B;
                }
                if (x2 > pxMax) {
                    x2 = pxMax;
                    y2 = (e.C - x2)/e.B;
                }
                if (x2 < pxMin) {
                    x2 = pxMin;
                    y2 = (e.C - x2)/e.B;
                }
            } else {
                x1 = pxMin;
                if (s1 != null && s1.X > pxMin) {
                    x1 = s1.X;
                }
                if (x1 > pxMax) {
                    return;
                }
                y1 = e.C - e.A*x1;
                x2 = pxMax;
                if (s2 != null && s2.X < pxMax) {
                    x2 = s2.X;
                }
                if (x2 < pxMin) {
                    return;
                }
                y2 = e.C - e.A*x2;
                if (((y1 > pyMax) && (y2 > pyMax)) || ((y1 < pyMin) && (y2 < pyMin))) {
                    return;
                }
                if (y1 > pyMax) {
                    y1 = pyMax;
                    x1 = (e.C - y1)/e.A;
                }
                if (y1 < pyMin) {
                    y1 = pyMin;
                    x1 = (e.C - y1) / e.A;
                }
                if (y2 > pyMax) {
                    y2 = pyMax;
                    x2 = (e.C - y2) / e.A;
                }
                if (y2 < pyMin) {
                    y2 = pyMin;
                    x2 = (e.C - y2) / e.A;
                }
            }
            Segments.Add(new Segment(x1, y1, x2, y2));
        }

        public void PlotTriple(Site s1, Site s2, Site s3) {
            Console.WriteLine("triple {0} {1} {2}", s1, s2, s3);
            Triangles.Add(new Triangle(s1, s2, s3));
        }
    }

    public class Segment {
        public PointF P1 { get; set; }
        public PointF P2 { get; set; }

        public Segment(float x1, float y1, float x2, float y2) {
            P1 = new PointF(x1, y1);
            P2 = new PointF(x2, y2);
        }
    }

    public class Triangle {
        public Triangle(Site s1, Site s2, Site s3) {
            V1 = s1;
            V2 = s2;
            V3 = s3;
        }
        public PointF V1 { get; set; }
        public PointF V2 { get; set; }
        public PointF V3 { get; set; }
    }
}