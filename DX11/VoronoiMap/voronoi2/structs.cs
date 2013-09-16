using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SlimDX;

namespace VoronoiMap.Voronoi2 {
    using System.Collections;
    using System.Diagnostics;
    using System.Linq;

    [DebuggerDisplay("{SiteID} :: {Coord.X}, {Coord.Y}")]
    public class Site {
        public Vector2 Coord;
        public int SiteID;

        public class Comparer : IComparer<Vector2> {
            public int Compare(Vector2 x, Vector2 y) {
                if (x.Y < y.Y) return -1;
                if (x.Y > y.Y) return 1;
                if (x.X < y.X) return -1;
                if (x.X > y.X) return 1;
                return 0;
            }
        }
    }

    public class Edge {
        public float A, B, C;
        public Dictionary<LR, Site> Vertices = new Dictionary<LR, Site>();
        public Dictionary<LR, Site> Sites = new Dictionary<LR, Site>();
        private Dictionary<LR, Vector2> _clippedVertices;
        public Dictionary<LR, Vector2> ClippedEnds { get { return _clippedVertices; } }
        public bool Visible {
            get { return _clippedVertices != null; }
        }
        public int EdgeID;
        public static readonly Edge Deleted = new Edge();

        internal Edge() {
            Vertices[LR.Left] = null;
            Vertices[LR.Right] = null;
            Sites[LR.Left] = null;
            Sites[LR.Right] = null;
        }

        public LineSegment DelaunayLine() {
            return new LineSegment(Sites[LR.Left].Coord, Sites[LR.Right].Coord);
        }

        public LineSegment VoronoiEdge() {
            if (Visible) {
                return new LineSegment(ClippedEnds[LR.Left], ClippedEnds[LR.Right]);
            }
            return new LineSegment(null, null);
        }
        public void ClipVertices(Rectangle bounds) {
            var xmin = bounds.Left;
            var ymin = bounds.Top;
            var xmax = bounds.Right;
            var ymax = bounds.Bottom;

            Site v0, v1;
            if (Equals(A, 1.0f) && B >= 0.0f) {
                v0 = Vertices[LR.Right];
                v1 = Vertices[LR.Left];
            } else {
                v0 = Vertices[LR.Left];
                v1 = Vertices[LR.Right];
            }

            float x0, x1, y0, y1;

            if (Equals(A, 1.0f)) {
                y0 = ymin;
                if (v0 != null && v0.Coord.Y > ymin) {
                    y0 = v0.Coord.Y;
                }
                if (y0 > ymax) {
                    return;
                }
                x0 = C - B * y0;
                y1 = ymax;
                if (v1 != null && v1.Coord.Y < ymax) {
                    y1 = v1.Coord.Y;
                }
                if (y1 < ymin) {
                    return;
                }
                x1 = C - B * y1;
                if ((x0 > xmax && x1 > xmax) || (x0 < xmin && x1 < xmin)) {
                    return;
                }
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
                if (v0 != null && v0.Coord.X > xmin) {
                    x0 = v0.Coord.X;
                }
                if (x0 > xmax) {
                    return;
                }
                y0 = C - A * x0;
                x1 = xmax;
                if (v1 != null && v1.Coord.X < xmax) {
                    x1 = v1.Coord.X;
                }
                if (x1 < xmin) {
                    return;
                }
                y1 = C - A * x1;
                if ((y0 > ymax && y1 > ymax) || (y0 < ymin && y1 < ymin)) {
                    return;
                }

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
            _clippedVertices = new Dictionary<LR, Vector2>();
            if (v0 == Vertices[LR.Left]) {
                _clippedVertices[LR.Left] = new Vector2(x0, y0);
                _clippedVertices[LR.Right] = new Vector2(x1, y1);
            } else {
                _clippedVertices[LR.Right] = new Vector2(x0, y0);
                _clippedVertices[LR.Left] = new Vector2(x1, y1);
            }
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

    public class HalfEdge {
        protected bool Equals(HalfEdge other) {
            return _index == other._index;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) {
                return false;
            }
            if (ReferenceEquals(this, obj)) {
                return true;
            }
            if (obj.GetType() != this.GetType()) {
                return false;
            }
            return Equals((HalfEdge)obj);
        }

        public override int GetHashCode() {
            return _index;
        }

        private static int nhe = 0;
        private readonly int _index;
        public HalfEdge EdgeListRight;
        public HalfEdge EdgeListLeft;
        public Edge Edge;
        public LR LeftRight;
        public Site Vertex;
        public float YStar;
        public HalfEdge PriorityQueueNext;

        internal HalfEdge() {
            _index = nhe++;
            
        }

        public static HalfEdge Create(Edge e, LR lr) {
            var answer = new HalfEdge {
                Edge = e,
                LeftRight = lr,
                PriorityQueueNext = null,
                Vertex = null,
                
            };
            return answer;

        }
    }

    public class LR {
        protected bool Equals(LR other) {
            return _lr == other._lr;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) {
                return false;
            }
            if (ReferenceEquals(this, obj)) {
                return true;
            }
            if (obj.GetType() != this.GetType()) {
                return false;
            }
            return Equals((LR)obj);
        }

        public override int GetHashCode() {
            return _lr;
        }

        public static LR Left = new LR(0);
        public static LR Right = new LR(1);
        private readonly int _lr;

        public static LR Other(LR lr) {
            return lr == Left ? Right : Left;
        }

        private LR(int i) {
            _lr = i;
        }

    }
}


