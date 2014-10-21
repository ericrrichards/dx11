using System;

namespace VoronoiMap {
    public class HalfEdge {
        public Edge Edge { get; set; }
        public Side Side { get; private set; }
        public Site Vertex { get; set; }
        public HalfEdge Left { get; set; }
        public HalfEdge Right { get; set; }
        public float YStar { get; set; }

        public HalfEdge(Edge edge, Side side) {
            Edge = edge;
            Side = side;
            Vertex = null;
            Left = null;
            Right = null;
        }

        public bool RightOf( Site p) {
            var e = Edge;
            var topSite = e.Region[Side.Right];
            var rightOfSite = (p.X > topSite.X);

            if (rightOfSite && (Side == Side.Left)) {
                return true;
            }
            if (!rightOfSite && (Side == Side.Right)) {
                return false;
            }
            bool above;
            if (Math.Abs(e.A - 1) < Geometry.Tolerance) {
                var dyp = p.Y - topSite.Y;
                var dxp = p.X - topSite.X;
                var fast = false;
                

                if ((!rightOfSite && (e.B < 0)) || (rightOfSite && (e.B >= 0))) {
                    above = fast = (dyp >= e.B*dxp);
                } else {
                    above = ((p.X + p.Y*e.B) > e.C);
                    if (e.B < 0) {
                        above = !above;
                    }
                    if (!above) {
                        fast = true;
                    }
                }
                if (!fast) {
                    var dxs = topSite.X - e.Region[Side.Left].X;
                    above = (e.B*(dxp*dxp - dyp*dyp)) < (dxs*dyp*(1 + 2*dxp/dxs + e.B*e.B));

                    if (e.B < 0) {
                        above = !above;
                    }
                }
            } else { // e.b == 1
                var y1 = e.C - e.A*p.X;
                var t1 = p.Y - y1;
                var t2 = p.X - topSite.X;
                var t3 = y1 - topSite.Y;

                above = (t1*t1) > (t2*t2 + t3*t3);
            }
            return Side == Side.Left ? above : !above;
        }
    }
}