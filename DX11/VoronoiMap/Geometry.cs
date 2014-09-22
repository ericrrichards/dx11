using System;
using System.Collections.Generic;
using System.Drawing;
using Core;

namespace VoronoiMap {
    public static class Geometry {
        public const double Tolerance = 1e-10;
        
        public static Edge Bisect(Site s1, Site s2) {
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

            var vertex = new Site(xint, yint);
            //vertex.AddEdge(e1);
            //vertex.AddEdge(e2);
            return vertex;
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

    public class Segment {
        public Site P1 { get; private set; }
        public Site P2 { get; private set; }
        public bool New { get; set; }

        public Segment(Site p1, Site p2) {
            P1 = p1;
            P2 = p2;
        }
    }

    public class Circle {
        private PointF Center { get; set; }
        private float Radius { get; set; }

        public Circle(PointF p1, PointF p2, PointF p3) {
            //http://stackoverflow.com/questions/4103405/what-is-the-algorithm-for-finding-the-center-of-a-circle-from-three-points
            var offset = p2.X * p2.X + p2.Y * p2.Y;
            var bc = (p1.X * p1.X + p1.Y * p1.Y - offset) / 2;
            var cd = (offset - p3.X * p3.X - p3.Y * p3.Y) / 2;
            var det = ((p1.X - p2.X) * (p2.Y - p3.Y)) - ((p2.X - p3.X) * (p1.Y - p2.Y));

            var iDet = 1.0f / det;

            var centerX = (bc * (p2.Y - p3.Y) - cd * (p1.Y - p2.Y)) * iDet;
            var centerY = (cd * (p1.X - p2.X) - bc * (p2.X - p3.X)) * iDet;
            var radius = MathF.Sqrt(((p2.X - centerX) * (p2.X - centerX)) + ((p2.Y - centerY) * (p2.Y - centerY)));
            Center = new PointF(centerX, centerY);
            Radius = radius;
        }

        public RectangleF GetRect() {
            var rectf = new RectangleF(Center.X - Radius, Center.Y - Radius, Radius * 2, Radius * 2);
            return rectf;
        }
    }

    public class Triangle {
        public Triangle(Site s1, Site s2, Site s3) {
            V1 = s1;
            V2 = s2;
            V3 = s3;
        }
        public Site V1 { get; private set; }
        public Site V2 { get; private set; }
        public Site V3 { get; private set; }
        public bool New { get; set; }
    }
    public enum WindingDirection {
        None = 0,
        Clockwise,
        CounterClockwise
    }

    public class Polygon {
        private List<Site> Vertices { get; set; }

        public Polygon(List<Site> vertices) {
            Vertices = vertices;
        }
        public float Area { get { return Math.Abs(SignedDoubleArea * 0.5f); } }

        public WindingDirection Winding {
            get {
                var sDoubleArea = SignedDoubleArea;
                if (sDoubleArea < 0) {
                    return WindingDirection.Clockwise;
                }
                if (sDoubleArea > 0) {
                    return WindingDirection.CounterClockwise;
                }
                return WindingDirection.None;
            }
        }

        public float SignedDoubleArea {
            get {
                var signedDoubleArea = 0.0f;
                for (int i = 0; i < Vertices.Count; i++) {
                    var nextIndex = (i + 1) % Vertices.Count;
                    var point = Vertices[i] ?? new Site(0,0);
                    var next = Vertices[nextIndex] ?? new Site(0,0);
                    signedDoubleArea += point.X * next.Y - next.X * point.Y;
                }
                return signedDoubleArea;
            }
        }
    }
}