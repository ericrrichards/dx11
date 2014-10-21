using System;
using System.Collections.Generic;
using System.Drawing;
using Core;

namespace VoronoiMap {
    public static class Geometry {
        public const double Tolerance = 1e-10;
        
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

        private float SignedDoubleArea {
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