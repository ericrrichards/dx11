using System;
using System.Collections.Generic;
using System.Diagnostics;
using SlimDX;

namespace VoronoiMap.Voronoi {
    public class Edge {
        public readonly static Edge Deleted = new Edge();
        private static int nedges;

        private readonly int _edgeIndex;

        private float _a, _b, _c;


        private Dictionary<LR, Site> _sites;
        private Vertex _leftVertex;
        private Vertex _rightVertex;
        private Dictionary<LR, Vector2> _clippedVertices;

        public Edge() {
            _edgeIndex = nedges++;
            Init();
        }

        public Site RightSite { get { return _sites[LR.Right]; } set { _sites[LR.Right] = value; } }
        public Site LeftSite { get { return _sites[LR.Left]; } set { _sites[LR.Left] = value; } }
        public float A { get { return _a; } }
        public float B { get { return _b; } }
        public float C { get { return _c; } }
        public Vertex LeftVertex { get { return _leftVertex; } }
        public Vertex RightVertex { get { return _rightVertex; } }
        public bool Visible {
            get { return _clippedVertices != null; }
        }

        public Dictionary<LR, Vector2> ClippedEnds { get { return _clippedVertices; } }

        private void Init() {
            _sites = new Dictionary<LR, Site>();
        }

        public Site Site(LR lr) {
            return _sites[lr];
        }

        public static Edge CreateBisectingEdge(Site s0, Site s1) {
            var dx = s1.X - s0.X;
            var dy = s1.Y - s0.Y;
            var absDx = dx > 0 ? dx : -dx;
            var abdDy = dy > 0 ? dy : -dy;
            var c = s0.X*dx + s0.Y*dy + (dx*dx + dy*dy)*0.5f;
            float a, b;

            if (absDx > abdDy) {
                a = 1.0f;
                b = dy/dx;
                c /= dx;
            } else {
                b = 1.0f;
                a = dx/dy;
                c /= dy;
            }
            var edge = new Edge {
                LeftSite = s0,
                RightSite = s1
            };
            s0.AddEdge(edge);
            s1.AddEdge(edge);
            edge._leftVertex = null;
            edge._rightVertex = null;
            edge._a = a;
            edge._b = b;
            edge._c = c;

            Console.WriteLine("CreateBisectingEdge: a {0} b {1} c {2} - {3}", edge._a, edge._b, edge._c, edge._edgeIndex);
            return edge;
        }

        public void SetVertex(LR leftRight, Vertex v) {
            if (leftRight == LR.Left) {
                _leftVertex = v;
            } else {
                _rightVertex = v;
            }
        }

        public void ClipVertices(Rectangle bounds) {
            var xmin = bounds.Left;
            var ymin = bounds.Top;
            var xmax = bounds.Right;
            var ymax = bounds.Bottom;

            Vertex v0, v1;
            if (Equals(_a, 1.0f) && _b >= 0.0f) {
                v0 = _rightVertex;
                v1 = _leftVertex;
            } else {
                v0 = _leftVertex;
                v1 = _rightVertex;
            }

            float x0, x1, y0, y1;

            if (Equals(_a, 1.0f)) {
                y0 = ymin;
                if (v0 != null && v0.Y > ymin) {
                    y0 = v0.Y;
                }
                if (y0 > ymax) {
                    return;
                }
                x0 = _c - _b*y0;
                y1 = ymax;
                if (v1 != null && v1.Y < ymax) {
                    y1 = v1.Y;
                }
                if (y1 < ymin) {
                    return;
                }
                x1 = _c - _b*y1;
                if ((x0 > xmax && x1 > xmax) || (x0 < xmin && x1 < xmin)) {
                    return;
                }
                if (x0 > xmax) {
                    x0 = xmax;
                    y0 = (_c - x0)/_b;
                } else if (x0 < xmin) {
                    x0 = xmin;
                    y0 = (_c - x0)/_b;
                }
                if (x1 > xmax) {
                    x1 = xmax;
                    y1 = (_c - x1)/_b;
                } else if (x1 < xmin) {
                    x1 = xmin;
                    y1 = (_c - x1)/_b;
                }
            } else {
                x0 = xmin;
                if (v0 != null && v0.X > xmin) {
                    x0 = v0.X;
                }
                if (x0 > xmax) {
                    return;
                }
                y0 = _c - _a*x0;
                x1 = xmax;
                if (v1 != null && v1.X < xmax) {
                    x1 = v1.X;
                }
                if (x1 < xmin) {
                    return;
                }
                y1 = _c - _a*x1;
                if ((y0 > ymax && y1 > ymax) || (y0 < ymin && y1 < ymin)) {
                    return;
                }

                if (y0 > ymax) {
                    y0 = ymax;
                    x0 = (_c - y0)/_a;
                } else if (y0 < ymin) {
                    y0 = ymin;
                    x0 = (_c - y0)/_a;
                }

                if (y1 > ymax) {
                    y1 = ymax;
                    x1 = (_c - y1)/_a;
                } else if (y1 < ymin) {
                    y1 = ymin;
                    x1 = (_c - y1)/_a;
                }

            }
            _clippedVertices = new Dictionary<LR, Vector2>();
            if (v0 == _leftVertex) {
                _clippedVertices[LR.Left] = new Vector2(x0, y0);
                _clippedVertices[LR.Right] = new Vector2(x1, y1);
            } else {
                _clippedVertices[LR.Right] = new Vector2(x0, y0);
                _clippedVertices[LR.Left] = new Vector2(x1, y1);
            }
        }

        public LineSegment DelaunayLine() {
            return new LineSegment(LeftSite.Coord, RightSite.Coord);
        }

        public LineSegment VoronoiEdge() {
            if (!Visible) return new LineSegment(null, null);
            return new LineSegment(_clippedVertices[LR.Left], _clippedVertices[LR.Right]);
        }
        public override string ToString() {
            return "Edge: " + _edgeIndex + "; sites " + _sites[LR.Left] + ", " + _sites[LR.Right]
                   + "; endVertices " + (_leftVertex != null ? LeftVertex.ToString() : "null") + ", " +
                   (_rightVertex != null ? _rightVertex.ToString() : "null") + "::";

        }
    }

    public class LineSegment {
        public LineSegment(Vector2? p0, Vector2? p1) {
            P0 = p0;
            P1 = p1;
        }

        public Vector2? P1 { get; set; }
        public Vector2? P0 { get; set; }
    }
}