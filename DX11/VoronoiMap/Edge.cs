using System;
using System.Drawing;
using Algorithms;

namespace VoronoiMap {
    public class Edge {
        public float A { get; private set; }
        public float B { get; private set; }
        public float C { get; private set; }

        public Site[] Region { get; private set; }
        public Site LeftSite { get { return Region[0]; } }
        public Site RightSite { get { return Region[1]; } }

        public Site[] Endpoint { get; private set; }
        public Site LeftVertex { get { return Endpoint[0]; } }
        public Site RightVertex { get { return Endpoint[1]; } }
        public Site[] ClippedEndpoints { get; private set; }

        private int EdgeNum { get; set; }

        public static int EdgeCount;
        public bool Visible { get { return ClippedEndpoints[Side.Left] != null && ClippedEndpoints[Side.Right] != null; } }
        public Segment DelauneyLine {
            get {
                return new Segment(Region[Side.Left], Region[Side.Right]);
            }
        }

        public Segment VoronoiEdge {
            get {
                return new Segment(ClippedEndpoints[Side.Left], ClippedEndpoints[Side.Right]);
            }
        }


        private Edge(Site s1, Site s2) {
            Region = new Site[2];
            Endpoint = new Site[2];
            ClippedEndpoints = new Site[2];

            Region[Side.Left] = s1;
            Region[Side.Right] = s2;
            EdgeNum = EdgeCount++;

            s1.AddEdge(this);
            s2.AddEdge(this);
        }
        public override string ToString() {
            return String.Format("#{7} A={0} B={1} C={2} Ep[L]={3} Ep[R]={4} R[L]={5}, R[R]={6}",
                                 A, B, C, Endpoint[0], Endpoint[1], Region[0], Region[1], EdgeNum);
        }

        private float SiteDistance() {
            return Site.Distance(Region[Side.Left], Region[Side.Right]);
        }

        private static int CompareSiteDistancesMax(Edge e0, Edge e1) {
            var l0 = e0.SiteDistance();
            var l1 = e1.SiteDistance();
            if (l0 < l1) {
                return 1;
            }
            if (l0 > l1) {
                return -1;
            }
            return 0;
        }

        public static int CompareSiteDistances(Edge e0, Edge e1) {
            return -CompareSiteDistancesMax(e0, e1);
        }
        public void ClipVertices(Rectangle bounds) {
            
            var clipped = GetClippedEnds(bounds);

            if (clipped == null) {
                return;
            }

            ClippedEndpoints[Side.Left] = new Site(clipped.Item1);
            ClippedEndpoints[Side.Right] = new Site(clipped.Item2);
        }

        public Tuple<PointF, PointF> GetClippedEnds(Rectangle bounds) {
            Site vertex0, vertex1;
            var xmin = bounds.X;
            var ymin = bounds.Y;
            var xmax = bounds.Right;
            var ymax = bounds.Bottom;


            float x0, x1, y0, y1;

            if (Math.Abs(A - 1.0) < Double.Epsilon && B >= 0.0) {
                vertex0 = RightVertex;
                vertex1 = LeftVertex;
            } else {
                vertex0 = LeftVertex;
                vertex1 = RightVertex;
            }

            if (Math.Abs(A - 1.0) < Double.Epsilon) {
                y0 = ymin;
                if (vertex0 != null && vertex0.Y > ymin)
                    y0 = vertex0.Y;
                if (y0 > ymax) {
                    return null;
                }

                x0 = C - B*y0;

                y1 = ymax;
                if (vertex1 != null && vertex1.Y < ymax)
                    y1 = vertex1.Y;
                if (y1 < ymin) {
                    return null;
                }
                x1 = C - B*y1;

                if ((x0 > xmax && x1 > xmax) || (x0 < xmin && x1 < xmin)) {
                    return null;
                }

                if (x0 > xmax) {
                    x0 = xmax;
                    y0 = (C - x0)/B;
                } else if (x0 < xmin) {
                    x0 = xmin;
                    y0 = (C - x0)/B;
                }

                if (x1 > xmax) {
                    x1 = xmax;
                    y1 = (C - x1)/B;
                } else if (x1 < xmin) {
                    x1 = xmin;
                    y1 = (C - x1)/B;
                }
            } else {
                x0 = xmin;
                if (vertex0 != null && vertex0.X > xmin)
                    x0 = vertex0.X;
                if (x0 > xmax) {
                    return null;
                }

                y0 = C - A*x0;

                x1 = xmax;
                if (vertex1 != null && vertex1.X < xmax)
                    x1 = vertex1.X;
                if (x1 < xmin) {
                    return null;
                }

                y1 = C - A*x1;

                if ((y0 > ymax && y1 > ymax) || (y0 < ymin && y1 < ymin)) {
                    return null;
                }
                if (y0 > ymax) {
                    y0 = ymax;
                    x0 = (C - y0)/A;
                } else if (y0 < ymin) {
                    y0 = ymin;
                    x0 = (C - y0)/A;
                }

                if (y1 > ymax) {
                    y1 = ymax;
                    x1 = (C - y1)/A;
                } else if (y1 < ymin) {
                    y1 = ymin;
                    x1 = (C - y1)/A;
                }
            }
            var clipped = CohenSutherland.ClipSegment(bounds, new PointF(x0, y0), new PointF(x1, y1));
            //Console.WriteLine("cl {0} {1} {2} {3}", x0, y0, x1, y1);

            return (vertex0 == LeftVertex) ? 
                new Tuple<PointF, PointF>(clipped.Item1, clipped.Item2) : 
                new Tuple<PointF, PointF>(clipped.Item2, clipped.Item1);
        }

        public static Edge CreateBisectingEdge(Site s1, Site s2) {
            var newEdge = new Edge(s1, s2);
            var dx = s2.X - s1.X;
            var dy = s2.Y - s1.Y;
            var adx = dx > 0 ? dx : -dx;
            var ady = dy > 0 ? dy : -dy;

            newEdge.C = s1.X * dx + s1.Y * dy + (dx * dx + dy * dy) * 0.5f;
            
            if (adx > ady) {
                newEdge.A = 1;
                newEdge.B = dy / dx;
                newEdge.C /= dx;
            } else {
                newEdge.B = 1;
                newEdge.A = dx / dy;
                newEdge.C /= dy;
            }
            
            return newEdge;
        }
    }
}
