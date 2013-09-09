using System;
using System.Collections.Generic;
using SlimDX;

namespace VoronoiMap.Voronoi {
    public class Edge {
        public readonly static Edge Deleted = new Edge();
        private static int _nedges = 0;

        private int _edgeIndex;

        private float a, b, c;


        private Dictionary<LR, Site> _sites;
        private Vertex _leftVertex;
        private Vertex _rightVertex;
        private Dictionary<LR, Vector2> _clippedVertices;

        public Edge() {
            _edgeIndex = _nedges++;
            Init();
        }

        public Site RightSite { get { return _sites[LR.Right]; } set { _sites[LR.Right] = value; } }
        public Site LeftSite { get { return _sites[LR.Left]; } set { _sites[LR.Left] = value; } }
        public float A { get { return a; } }
        public float B { get { return b; } }
        public float C { get { return c; } }
        public Vertex LeftVertex { get { return _leftVertex; } }
        public Vertex RightVertex { get { return _rightVertex; } }

        private void Init() {
            _sites = new Dictionary<LR, Site>();
        }

        public Site Site(LR lr) {
            return _sites[lr];
        }

        public static Edge CreateBisectingEdge(Site s0, Site s1) {
            var dx = s1.X - s0.X;
            var dy = s1.Y - s0.Y;
            var absDx = Math.Abs(dx);
            var abdDy = Math.Abs(dy);
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
            edge.a = a;
            edge.b = b;
            edge.c = c;
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
            if (Equals(a, 1.0f) && b >= 0.0f) {
                v0 = _rightVertex;
                v1 = _leftVertex;
            } else {
                v0 = _leftVertex;
                v1 = _rightVertex;
            }

            float x0, x1, y0, y1;

            if (Equals(a, 1.0f)) {
                y0 = ymin;
                if (v0 != null && v0.Y > ymin) {
                    y0 = v0.Y;
                }
                if (y0 > ymax) {
                    return;
                }
                x0 = c - b*y0;
                y1 = ymax;
                if (v1 != null && v1.Y < ymax) {
                    y1 = v1.Y;
                }
                if (y1 < ymin) {
                    return;
                }
                x1 = c - b*y1;
                if ((x0 > xmax && x1 > xmax) || (x0 < xmin && x1 < xmin)) {
                    return;
                }
                if (x0 > xmax) {
                    x0 = xmax;
                    y0 = (c - x0)/b;
                } else if (x0 < xmin) {
                    x0 = xmin;
                    y0 = (c - x0)/b;
                }
                if (x1 > xmax) {
                    x1 = xmax;
                    y1 = (c - x1)/b;
                } else if (x1 < xmin) {
                    x1 = xmin;
                    y1 = (c - x1)/b;
                }
            } else {
                x0 = xmin;
                if (v0 != null && v0.X > xmin) {
                    x0 = v0.X;
                }
                if (x0 > xmax) {
                    return;
                }
                y0 = c - a*x0;
                x1 = xmax;
                if (v1 != null && v1.X < xmax) {
                    x1 = v1.X;
                }
                if (x1 < xmin) {
                    return;
                }
                y1 = c - a*x1;
                if ((y0 > ymax && y1 > ymax) || (y0 < ymin && y1 < ymin)) {
                    return;
                }

                if (y0 > ymax) {
                    y0 = ymax;
                    x0 = (c - y0)/a;
                } else if (y0 < ymin) {
                    y0 = ymin;
                    x0 = (c - y0)/a;
                }

                if (y1 > ymax) {
                    y1 = ymax;
                    x1 = (c - y1)/a;
                } else if (y1 < ymin) {
                    y1 = ymin;
                    x1 = (c - y1)/a;
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
    }
}