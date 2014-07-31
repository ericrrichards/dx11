using System;

namespace Algorithms.Voronoi {
    public class Vertex : ICoord {
        public static Vertex Create(float x, float y) {
            if (float.IsNaN(x) || float.IsNaN(y)) {
                return VertexAtInfinity;
            }
            var v = new Vertex(x, y);
            return v;
        }

        private int _vertexIndex;
        public int VertexIndex { get { return _vertexIndex; }  }

        public void SetIndex() {
            _vertexIndex = _numVertices++;
            Console.WriteLine(this);
        }
        public override string ToString() {
            return string.Format("vertex({0}) at {1} {2}", VertexIndex, X, Y);
        }

        public static Vertex Intersect(HalfEdge he0, HalfEdge he1) {
            var e1 = he0.Edge;
            var e2 = he1.Edge;

            if (e1 == null || e2 == null) {
                return null;
            }
            if (e1.RightSite == e2.RightSite) {
                return null;
            }

            var determinant = e1.A*e2.B - e1.B*e2.A;
            if (-1.0e-10 < determinant && determinant < 1.0e-10) {
                // the edges are parallel
                return null;
            }
            var iX = (e1.C*e2.B - e2.C*e1.B)/determinant;
            var iY = (e2.C*e1.A - e1.C*e2.A)/determinant;

            HalfEdge he;
            Edge e;
            if (Voronoi.CompareByYThenX(e1.RightSite, e2.RightSite) < 0) {
                he = he0;
                e = e1;
            } else {
                he = he1;
                e = e2;
            }
            var rightOfSite = (iX >= e.RightSite.X);
            if ((rightOfSite && he.LeftRight == LR.Side.Left) || (!rightOfSite && he.LeftRight == LR.Side.Right)) {
                return null;
            }
            return Create(iX, iY);
        }
        public static readonly Vertex VertexAtInfinity = new Vertex(float.NaN, float.NaN);

        private Vertex(float x, float y) {
            Init(x, y);
        }

        private Vertex Init(float x, float y) {
            Coord = new Point(x,y);
            return this;
        }

        private static int _numVertices = 0;
        
    }
}