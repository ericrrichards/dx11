using System;
using System.Collections.Generic;
using System.Drawing;
using Core;

namespace Algorithms.Voronoi {
    public class Point {
        public float X { get; set; }
        public float Y { get; set; }

        public Point(float x, float y) {
            X = x;
            Y = y;
        }

        public static float Distance(Point p1, Point p2) {
            var dx = p1.X - p2.X;
            var dy = p1.Y - p2.Y;
            return MathF.Sqrt(dx * dx + dy * dy);
        }

        public static Point Interpolate(Point p1, Point p2, float prc) {
            return new Point(p1.X + prc * (p2.X - p1.X), p1.Y + prc * (p2.Y - p1.Y));
        }

        public static implicit operator PointF(Point p) {
            return new PointF(p.X, p.Y);
        } 
    }

    public class Triangle {
        private Site A { get; set; }
        private Site B { get; set; }
        private Site C { get; set; }

        public Site[] Sites { get { return new[] { A, B, C }; } }

        public Triangle(Site a, Site b, Site c) {
            A = a;
            B = b;
            C = c;
        }
    }

    public class Circle {
        public Point Center { get; set; }
        public float Radius { get; set; }

        public Circle(float x, float y, float r) {
            Center = new Point(x, y);
            Radius = r;
        }
    }

    public class Rectangle {
        public float X { get; private set; }
        public float Y { get; private set; }
        public float Width { get; private set; }
        public float Height { get; private set; }
        public float Bottom { get { return Y + Height; } }
        public float Left { get { return X; } }
        public float Right { get { return X + Width; } }
        public float Top { get { return Y; } }

        public Rectangle(float x, float y, float width, float height) {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public Rectangle(Rectangle r) {
            X = r.X;
            Y = r.Y;
            Width = r.Width;
            Height = r.Height;
        }
    }

    public class LineSegment {
        public Point P0 { get; set; }
        public Point P1 { get; set; }

        public static int CompareLengthsMax(LineSegment a, LineSegment b) {
            var l0 = Point.Distance(a.P0, a.P1);
            var l1 = Point.Distance(b.P0, b.P1);

            if (l0 < l1) {
                return 1;
            }
            if (l0 > l1) {
                return -1;
            }
            return 0;
        }
        public static int CompareLengths(LineSegment a, LineSegment b) { return -CompareLengthsMax(a, b); }

        public LineSegment(Point p0, Point p1) {
            P0 = p0;
            P1 = p1;
        }
    }

    public enum WindingDirection {
        None = 0,
        Clockwise,
        CounterClockwise
    }

    public class Polygon {
        private List<Point> Vertices { get; set; }

        public Polygon(List<Point> vertices) {
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
                    var point = Vertices[i];
                    var next = Vertices[nextIndex];
                    signedDoubleArea += point.X * next.Y - next.X * point.Y;
                }
                return signedDoubleArea;
            }
        }
    }
}
