using SlimDX;

namespace VoronoiMap.Voronoi {
    public class HalfEdge {
        public HalfEdge NextInPriorityQueue;
        public HalfEdge EdgeListLeftNeighbor;
        public HalfEdge EdgeListRightNeighbor;

        public Edge Edge;
        public LR LeftRight;
        public Vertex Vertex;

        public float YStar;

        public HalfEdge(Edge edge, LR lr) {
            Init(edge, lr);
        }

        private void Init(Edge edge, LR lr) {
            Edge = edge;
            LeftRight = lr;
            NextInPriorityQueue = null;
            Vertex = null;
        }

        public HalfEdge() : this(null, null) {
        }
        public override string ToString() {
            return string.Format("HalfEdge (leftright: {0}; vertex: {1})", LeftRight, Vertex);
        }

        public bool IsLeftOf(Vector2 p) {
            bool above;
            var topSite = Edge.RightSite;
            var rightOfSite = p.X > topSite.X;
            if (rightOfSite && LeftRight == LR.Left) {
                return true;
            }
            if (!rightOfSite && LeftRight == LR.Right) {
                return false;
            }
            if (Equals(Edge.A, 1.0f)) {
                var dyp = p.Y - topSite.Y;
                var dxp = p.X - topSite.X;
                var fast = false;
                if ((!rightOfSite && Edge.B < 0.0f) || (rightOfSite && Edge.B >= 0.0f)) {
                    above = dyp >= Edge.B*dxp;
                    fast = above;
                } else {
                    above = p.X + p.Y*Edge.B > Edge.C;
                    if (Edge.B < 0.0f) {
                        above = !above;

                    }
                    if (!above) {
                        fast = true;
                    }
                }
                if (!fast) {
                    var dxs = topSite.X - Edge.LeftSite.X;
                    above = Edge.B*(dxp*dxp - dyp*dyp) < dxs*dyp*(1.0f + 2.0f*dxp/dxs + Edge.B*Edge.B);
                    if (Edge.B < 0.0f) {
                        above = !above;
                    }
                }
            } else {
                var y1 = Edge.C - Edge.A * p.X;
                var t1 = p.Y - y1;
                var t2 = p.X - topSite.X;
                var t3 = y1 - topSite.Y;
                above = t1*t2 > t2*t2 + t3*t3;
            }
            return LeftRight == LR.Left ? above : !above;
        }
    }
}