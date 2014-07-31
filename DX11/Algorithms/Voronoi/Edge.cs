using System;
using System.Collections.Generic;

namespace Algorithms.Voronoi {
    public class Edge {
        public LineSegment DelauneyLine {
            get {
                return new LineSegment(LeftSite.Coord, RightSite.Coord);
            }
        }

        public LineSegment VoronoiEdge {
            get {
                if (!Visible) {
                    return new LineSegment(null,null);
                }
                return new LineSegment(ClippedEnds[LR.Side.Left], ClippedEnds[LR.Side.Right]);
            }
        }
        public static Edge CreateBisectingEdge(Site s0, Site s1) {
            var edge = Create();
            edge.LeftSite = s0;
            edge.RightSite = s1;
            s0.AddEdge(edge);
            s1.AddEdge(edge);

            var dx = s1.X - s0.X;
            var dy = s1.Y - s0.Y;
            var absdx = dx > 0 ? dx : -dx;
            var absdy = dy > 0 ? dy : -dy;
            edge.C = s0.X*dx + s0.Y*dy + (dx*dx + dy*dy)*0.5f;
            if (absdx > absdy) {
                edge.A = 1.0f;
                edge.B = dy/dx;
                edge.C /= dx;
            } else {
                edge.B = 1.0f;
                edge.A = dx/dy;
                edge.C /= dy;
            }
            Console.WriteLine(edge);

            return edge;
        }
        public Vertex LeftVertex { get; private set; }
        public Vertex RightVertex { get; private set; }

        public Vertex Vertex(LR.Side leftRight) {
            return (leftRight == LR.Side.Left) ? LeftVertex : RightVertex;
        }

        public void SetVertex(LR.Side leftRight, Vertex v) {
            if (leftRight == LR.Side.Left) {
                LeftVertex = v;
            } else {
                RightVertex = v;
            }
            if (Vertex(LR.Other(leftRight)) != null) {
                OutEnd();
            }
        }
        public bool IsPartOfConvexHull { get { return LeftVertex == null || RightVertex == null; } }

        public float SiteDistance() {
            return Point.Distance(LeftSite.Coord, RightSite.Coord);
        }

        public static int CompareSiteDistancesMax(Edge e0, Edge e1) {
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
        public Dictionary<LR.Side, Point> ClippedEnds { get; private set; }

        // unless the entire Edge is outside the bounds.
        // In that case visible will be false:
        public bool Visible { get { return ClippedEnds[LR.Side.Left] != null && ClippedEnds[LR.Side.Right] != null; } }
        public Site LeftSite { get { return Sites[LR.Side.Left]; } set { Sites[LR.Side.Left] = value; } }
        public Site RightSite { get { return Sites[LR.Side.Right]; } set { Sites[LR.Side.Right] = value; } }
        public Dictionary<LR.Side, Site> Sites { get; private set; }

        public void ClipVertices(Rectangle bounds) {
            var xmin = bounds.X;
            var ymin = bounds.Y;
            var xmax = bounds.Right;
            var ymax = bounds.Bottom;

            Vertex vertex0, vertex1;
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
                if (y0 > ymax)
                    return;
                x0 = C - B * y0;

                y1 = ymax;
                if (vertex1 != null && vertex1.Y < ymax)
                    y1 = vertex1.Y;
                if (y1 < ymin)
                    return;
                x1 = C - B * y1;

                if ((x0 > xmax && x1 > xmax) || (x0 < xmin && x1 < xmin))
                    return;

                if (x0 > xmax) {
                    x0 = xmax;
                    y0 = (C - x0) / B;
                } else if (x0 < xmin) {
                    x0 = xmin;
                    y0 = (C - x0) / B;
                }

                if (x1 > xmax) {
                    x1 = xmax;
                    y1 = (C - x1) / B;
                } else if (x1 < xmin) {
                    x1 = xmin;
                    y1 = (C - x1) / B;
                }
            } else {
                x0 = xmin;
                if (vertex0 != null && vertex0.X > xmin)
                    x0 = vertex0.X;
                if (x0 > xmax)
                    return;
                y0 = C - A * x0;

                x1 = xmax;
                if (vertex1 != null && vertex1.X < xmax)
                    x1 = vertex1.X;
                if (x1 < xmin)
                    return;
                y1 = C - A * x1;

                if ((y0 > ymax && y1 > ymax) || (y0 < ymin && y1 < ymin))
                    return;

                if (y0 > ymax) {
                    y0 = ymax;
                    x0 = (C - y0) / A;
                } else if (y0 < ymin) {
                    y0 = ymin;
                    x0 = (C - y0) / A;
                }

                if (y1 > ymax) {
                    y1 = ymax;
                    x1 = (C - y1) / A;
                } else if (y1 < ymin) {
                    y1 = ymin;
                    x1 = (C - y1) / A;
                }
            }
            if (vertex0 == LeftVertex) {
                ClippedEnds[LR.Side.Left] = new Point(x0, y0);
                ClippedEnds[LR.Side.Right] = new Point(x1, y1);
            } else {
                ClippedEnds[LR.Side.Right] = new Point(x0, y0);
                ClippedEnds[LR.Side.Left] = new Point(x1, y1);
            }
            Console.WriteLine("cl {0} {1} {2} {3}", x0, y0, x1, y1);
        }

        public override string ToString() {
            return string.Format("line({0}) {1}x+{2}y={3} bisecting {4} {5}", EdgeIndex, A, B, C, LeftSite.SiteIndex, RightSite.SiteIndex);
        }

        public void OutEnd() {
            Console.WriteLine("e {0} {1} {2}", EdgeIndex, LeftVertex!=null? LeftVertex.VertexIndex : -1, RightVertex!=null ? RightVertex.VertexIndex : -1);
        }

        // the equation of the edge: ax + by = c
        public float A { get; set; }
        public float B { get; set; }
        public float C { get; set; }

        public static Edge DELETED = new Edge();




        private Edge() {
            EdgeIndex = _numEdges++;
            Init();
        }

        private static Edge Create() {
            return new Edge();
        }

        private void Init() {
            RightVertex = null;
            LeftVertex = null;
            ClippedEnds = new Dictionary<LR.Side, Point>(){{LR.Side.Left, null}, {LR.Side.Right, null}};
            Sites = new Dictionary<LR.Side, Site>(){ {LR.Side.Left, null}, {LR.Side.Right, null}};
        }

        private int EdgeIndex { get; set; }
        private static int _numEdges = 0;

    }
}