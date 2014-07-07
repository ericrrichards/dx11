using System;

namespace VoronoiMap {
    public static class Geometry {
        public const double Tolerance = 1e-10;
        
        public static Edge Bisect(Site s1, Site s2) {
            var newEdge = new Edge(s1, s2);
            var dx = s2.X - s1.X;
            var dy = s2.Y - s1.Y;
            var adx = Math.Abs(dx);
            var ady = Math.Abs(dy);

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

        public static Site Intersect(HalfEdge el1, HalfEdge el2) {
            var e1 = el1.Edge;
            var e2 = el2.Edge;
            if (e1 == null || e2 == null) {
                return null;
            }
            if (e1.Region[Side.Right] == e2.Region[Side.Right]) {
                return null;
            }
            var d = (e1.A*e2.B) - (e1.B*e2.A);
            if (Math.Abs(d) < Tolerance) {
                return null;
            }
            var xint = (e1.C*e2.B - e2.C*e1.B)/d;
            var yint = (e2.C*e1.A - e1.C*e2.A)/d;

            var e1Region = e1.Region[Side.Right];
            var e2Region = e2.Region[Side.Right];

            HalfEdge el;
            Edge e;
            if ((e1Region.Y < e2Region.Y) || Math.Abs(e1Region.Y - e2Region.Y) < Tolerance && e1Region.X < e2Region.X) {
                el = el1;
                e = e1;
            } else {
                el = el2;
                e = e2;
            }
            var rightOfSite = (xint >= e.Region[Side.Right].X);
            if ((rightOfSite && (el.Side == Side.Left)) || (!rightOfSite && (el.Side == Side.Right))) {
                return null;
            }

            return new Site(xint, yint);
        }

        public static bool RightOf(HalfEdge he, Site p) {
            var e = he.Edge;
            var topSite = e.Region[Side.Right];
            var rightOfSite = (p.X > topSite.X);

            if (rightOfSite && (he.Side == Side.Left)) {
                return true;
            }
            if (!rightOfSite && (he.Side == Side.Right)) {
                return false;
            }
            bool above;
            if (Math.Abs(e.A - 1) < Tolerance) {
                var dyp = p.Y - topSite.Y;
                var dxp = p.X - topSite.X;
                var fast = false;
                

                if ((!rightOfSite && (e.B < 0)) || (rightOfSite && (e.B >= 0))) {
                    above = fast = (dyp > - e.B*dxp);
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
            return he.Side == Side.Left ? above : !above;
        }

        public static void EndPoint(Edge edge, Side side, Site site, VoronoiGraph c) {
            edge.Endpoint[side] = site;
            var opSide = side == Side.Left ? Side.Right : Side.Left;
            if (edge.Endpoint[opSide] == null) {
                return;
            }
            c.PlotEndpoint(edge);
        }

        public static float Distance(Site s, Site t) {
            var dx = s.X - t.X;
            var dy = s.Y - t.Y;
            return (float) Math.Sqrt(dx*dx + dy*dy);
        }
    }
}