using SlimDX;

namespace VoronoiMap.Voronoi {
    public class Vertex : ICoord {
        private int _vertexIndex;
        private static int _nVertices = 0;
        public static readonly ICoord VertexAtInfinity = new Vertex(float.NaN, float.NaN);

        private Vertex(float x, float y) {
            Init(x, y);
        }

        private void Init(float x, float y) {
            Coord = new Vector2(x,y);
        }

        public static Vertex Intersect(HalfEdge he0, HalfEdge he1) {
            var edge0 = he0.Edge;
            var edge1 = he1.Edge;

            if (edge0 == null || edge1 == null) return null;
            if (edge0.RightSite == edge1.RightSite) return null;

            var determinant = edge0.A*edge1.B - edge0.B*edge1.A;

            if (-1.0e-10 < determinant && determinant < 1.0e-10) {
                return null;
            }
            var intersectionX = (edge0.C*edge1.B - edge1.C*edge0.B)/determinant;
            var intersectionY = (edge1.C*edge0.A - edge0.C * edge1.A)/determinant;

            HalfEdge halfEdge;
            Edge edge;
            if (Voronoi.CompareByYThenX(edge0.RightSite, edge1.RightSite) < 0) {
                halfEdge = he0;
                edge = edge0;
            } else {
                halfEdge = he1;
                edge = edge1;
            }
            var rightOfSite = intersectionX > edge.RightSite.X;
            if ((rightOfSite && halfEdge.LeftRight == LR.Left) || (!rightOfSite && halfEdge.LeftRight == LR.Right)) {
                return null;
            }
            return new Vertex(intersectionX, intersectionY);

        }

        public void SetIndex() {
            _vertexIndex = _nVertices++;
        }
    }
}