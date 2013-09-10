using System;
using System.Collections.Generic;
using SlimDX;

namespace VoronoiMap.Voronoi {
    public enum Winding {
        Clockwise,
        CounterClockwise,
        None
    }

    public class Polygon {
        private readonly List<Vector2> _vertices;

        public Polygon(List<Vector2> vertices) {
            _vertices = vertices;
        }

        public Winding Winding {
            get { 
                var signedDoubleArea = SignedDoubleArea();
                if (signedDoubleArea < 0) {
                    return Winding.Clockwise;
                }
                if (signedDoubleArea > 0) {
                    return Winding.CounterClockwise;
                }
                return Winding.None;
            } 
        }

        private float SignedDoubleArea() {
            var n = _vertices.Count;
            var signedDoubleArea = 0.0f;
            for (int index = 0; index < n; index++) {
                var nextIndex = (index + 1)%n;
                var point = _vertices[index];
                var next = _vertices[nextIndex];
                signedDoubleArea += point.X*next.Y - next.X*point.Y;
            }
            return signedDoubleArea;
        }
        public float Area() {
            return Math.Abs(SignedDoubleArea() * 0.5f);
        }
    }
}