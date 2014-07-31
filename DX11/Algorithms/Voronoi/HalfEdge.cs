using System;

namespace Algorithms.Voronoi {
    public class HalfEdge {
        public static HalfEdge Create(Edge edge, LR.Side lr) {
            var he = new HalfEdge(edge, lr);
            return he;
        }

        public static HalfEdge CreateDummy() {
            return Create(null, LR.Side.Left);
        }

        public bool IsLeftOf(Point p) {
            bool above;

            var topSite = Edge.RightSite;
            var rightOfSite = p.X > topSite.X;
            if (rightOfSite && LeftRight == LR.Side.Left) {
                return true;
            }
            if (!rightOfSite && LeftRight == LR.Side.Right) {
                return false;
            }

            if (Math.Abs(Edge.A - 1.0f) < float.Epsilon) {
                var dyp = p.Y - topSite.Y;
                var dxp = p.X - topSite.X;
                var fast = false;
                if ((!rightOfSite && Edge.B < 0.0f) || (rightOfSite && Edge.B >= 0.0f)) {
                    above = dyp >= Edge.B*dxp;
                    fast = above;
                } else {
                    above = ((p.X + p.Y*Edge.B) > Edge.C);
                    if (Edge.B < 0.0f) {
                        above = !above;
                    }
                    if (!above) {
                        fast = true;
                    }
                }
                if (!fast) {
                    var dxs = topSite.X - Edge.LeftSite.X;
                    above = (Edge.B*(dxp*dxp - dyp*dyp)) < (dxs*dyp*(10f + 2.0f*dxp/dxs + Edge.B*Edge.B));
                    if (Edge.B < 0.0) {
                        above = !above;
                    }
                }
            } else {/* edge.b == 1.0 */
                var y1 = Edge.C - Edge.A*p.X;
                var t1 = p.Y - y1;
                var t2 = p.X - topSite.X;
                var t3 = y1 - topSite.Y;
                above = (t1*t1) > (t2*t2 + t3*t3);
            }
            return (LeftRight == LR.Side.Left ? above : !above);
        }

        public HalfEdge EdgeListLeftNeighbor { get; set; }
        public HalfEdge EdgeListRightNeighbor { get; set; }
        public HalfEdge NextInPriorityQueue { get; set; }

        public Edge Edge { get; set; }
        public LR.Side LeftRight { get; set; }
        public Vertex Vertex { get; set; }
        public float YStar { get; set; }

        private HalfEdge(Edge edge, LR.Side lr) {
            Init(edge, lr);
        }

        HalfEdge Init(Edge edge, LR.Side lr) {
            Edge = edge;
            LeftRight = lr;
            EdgeListLeftNeighbor = null;
            EdgeListRightNeighbor = null;
            NextInPriorityQueue = null;
            Vertex = null;
            YStar = 0;
            return this;
        }
    }
}