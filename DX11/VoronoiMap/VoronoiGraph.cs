using System;
using System.Collections.Generic;
using System.Reflection;
using log4net;

namespace VoronoiMap {
    public class VoronoiGraph {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public bool Debug { get; set; }
        private int Width { get; set; }
        public int Height { get; private set; }

        public readonly List<Site> Sites = new List<Site>();
        public readonly List<Site> Vertices = new List<Site>();
        public readonly List<Segment> Segments = new List<Segment>(); 
        public readonly List<Triangle> Triangles = new List<Triangle>();

        public float SweepLine { get; set; }

        public VoronoiGraph(int width=800, int height=600) {
            Width = width;
            Height = height;
            Debug = false;
        }

        public void PlotSite(Site site) {
            site.New = true;
            Sites.Add(site);
            if (Debug) {
                Console.WriteLine("site {0}", site);
                Log.InfoFormat("site {0}", site);
            }
            if (site.Y > SweepLine) {
                SweepLine = site.Y;
            }
        }

        public void PlotBisector(Edge e) {
            if (Debug) {
                Console.WriteLine("bisector {0} {1}", e.Region[Side.Left], e.Region[Side.Right]);
                Log.InfoFormat("bisector {0} {1}", e.Region[Side.Left], e.Region[Side.Right]);
            }
        }

        public void PlotEndpoint(Edge e) {
            if (Debug) {
                Console.WriteLine("EP {0}", e);
                Log.InfoFormat("EP {0}", e);
            }
            ClipLine(e);
        }

        public void PlotVertex(Site s) {
            if (Debug) {
                Console.WriteLine("vertex {0},{1}", s.X, s.Y);
                Log.InfoFormat("vertex {0},{1}", s.X, s.Y);
            }
            s.New = true;
            Vertices.Add(s);
        }

        public void PlotTriple(Site s1, Site s2, Site s3) {
            if (Debug) {
                Console.WriteLine("triple {0} {1} {2}", s1, s2, s3);
            }
            var triangle = new Triangle(s1, s2, s3) { New= true};
            Triangles.Add(triangle);
        }

        public void ResetNewItems() {
            foreach (var site in Sites) {
                site.New = false;
            }
            foreach (var vertex in Vertices) {
                vertex.New = false;
            }
            foreach (var segment in Segments) {
                segment.New = false;
            }
            foreach (var triangle in Triangles) {
                triangle.New = false;
            }
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
            Segments.Add(new Segment(x1, y1, x2, y2) { New = true});
        }

        
    }

    
}