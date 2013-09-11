using System;
using SlimDX;

namespace VoronoiMap.voronoi2 {
    public static class Geometry {
        public static int NumVertices;
        public static int NumEdges;
        public static int SqrtNumSites;
        public static float DeltaX;
        public static float DeltaY;
        public static Rectangle Bounds;

        public static void Init(int numSites, Rectangle bounds) {
            NumVertices = NumEdges = 0;
            SqrtNumSites = (int)Math.Sqrt(numSites + 4);
            DeltaX = bounds.Left - bounds.Right;
            DeltaY = bounds.Bottom - bounds.Top;
            Bounds = bounds;
        }

        internal static Edge Bisect(Site s1, Site s2) {
            var newEdge = new Edge();
            newEdge.Reg[LR.Left] = s1;
            newEdge.Reg[LR.Right] = s2;

            newEdge.EP[LR.Left] = newEdge.EP[LR.Right] = null;
            var dx = s2.Coord.X - s1.Coord.X;
            var dy = s2.Coord.Y - s1.Coord.Y;
            var adx = dx > 0 ? dx : -dx;
            var ady = dy > 0 ? dy : -dy;
            newEdge.C = s1.Coord.X*dx + s1.Coord.Y*dy + (dx*dx + dy*dy)*0.5f;
            if (adx > ady) {
                newEdge.A = 1.0f;
                newEdge.B = dy/dx;
                newEdge.C /= dx;
            } else {
                newEdge.B = 1.0f;
                newEdge.A = dy/dx;
                newEdge.C /= dy;
            }
            newEdge.EdgeID = NumEdges;
            Out.Bisector(newEdge);
            NumEdges++;
            return newEdge;
        }

        internal static Site Intersect(HalfEdge el1, HalfEdge el2) {
            var e1 = el1.Edge;
            var e2 = el2.Edge;
            if ((e1 == null) || e2 == null) {
                return null;
            }
            if (e1.Reg[LR.Right] == e2.Reg[LR.Right]) {
                return null;
            }
            var d = (e1.A*e2.B) - (e1.B*e2.A);
            if ((-1.0e-10 < d) && (d < 1.0e-10)) {
                return null;
            }
            var xint = (e1.C*e2.B - e2.C*e1.B)/d;
            var yint = (e2.C*e1.A - e1.C*e2.A)/d;
            HalfEdge el;
            Edge e;
            if ((e1.Reg[LR.Right].Coord.Y < e2.Reg[LR.Right].Coord.Y) ||
                (e1.Reg[LR.Right].Coord.Y == e2.Reg[LR.Right].Coord.Y &&
                 e1.Reg[LR.Right].Coord.X < e2.Reg[LR.Right].Coord.X)) {
                el = el1;
                e = e1;
            } else {
                el = el2;
                e = e2;
            }
            var rightOfSite = (xint >= e.Reg[LR.Right].Coord.X);
            if ((rightOfSite && (el.LeftRight == LR.Left)) ||
                (!rightOfSite && (el.LeftRight == LR.Right))) {
                return null;
            }
            var v = new Site();
            v.Coord = new Vector2(xint, yint);
            return v;

        }

        internal static bool RightOf(HalfEdge el, Vector2 p) {
            var e = el.Edge;
            var topSite = e.Reg[LR.Right];
            var rightOfSite = (p.X > topSite.Coord.X);
            if (rightOfSite && (el.LeftRight == LR.Left)) {
                return true;
            }
            if (!rightOfSite && (el.LeftRight == LR.Right)) {
                return false;
            }
            bool above;
            if (e.A == 1.0f) {
                var dyp = p.Y - topSite.Coord.Y;
                var dxp = p.X - topSite.Coord.X;
                var fast = false;
                if ((!rightOfSite && (e.B < 0.0f)) || (rightOfSite && (e.B >= 0.0f))) {
                    fast = above = dyp >= e.B*dxp;
                } else {
                    above = ((p.X + p.Y*e.B) > e.C);
                    if (e.B < 0.0f) {
                        above = !above;
                    }
                    if (!above) {
                        fast = true;
                    }
                }
                if (!fast) {
                    var dxs = topSite.Coord.X - e.Reg[LR.Left].Coord.X;
                    above = (e.B*(dxp*dxp - dyp*dyp)) < (dxs*dyp*(1.0f + 2.0f*dxp/dxs + e.B*e.B));
                    if (e.B < 0.0f) {
                        above = !above;
                    }
                }
            } else {
                var yl = e.C - e.A*p.X;
                var t1 = p.Y - yl;
                var t2 = p.X - topSite.Coord.X;
                var t3 = yl - topSite.Coord.Y;
                above = ((t1*t1) > ((t2*t2) + (t3*t3)));
            }
            return (el.LeftRight == LR.Left ? above : !above);
        }
        public static void Endpoint(Edge e, LR lr, Site s) {
            e.EP[lr] = s;
            if (e.EP[LR.Other(lr)] == null) {
                return;
            }
            Out.Endpoint(e);
        }
        public static float Dist(Site s, Site t) {
            var dx = s.Coord.X - t.Coord.X;
            var dy = s.Coord.Y - t.Coord.Y;
            return (float) Math.Sqrt(dx*dx + dy*dy);
        }
        public static void Makevertex(Site v) {
            v.SiteID = NumVertices++;
            Out.Vertex(v);
        }

    }
}