using SlimDX;

namespace VoronoiMap.Voronoi {
    public class HalfEdge {
        public HalfEdge NextInPriorityQueue;
        public HalfEdge EdgeListLeftNeighbor;
        public HalfEdge EdgeListRightNeighbor;

        public Edge Edge;
        public LR LeftRight;
        public Vertex Vertex;

        public float YStar;

        public HalfEdge(Edge edge, LR lr) {
            Init(edge, lr);
        }

        private void Init(Edge edge, LR lr) {
            Edge = edge;
            LeftRight = lr;
            NextInPriorityQueue = null;
            Vertex = null;
        }

        public HalfEdge() : this(null, null) {
        }
        public override string ToString() {
            return string.Format("HalfEdge (leftright: {0}; vertex: {1})", LeftRight, Vertex);
        }

        public bool IsLeftOf(Vector2 p) {
            var topSite = Edge.RightSite;
        }
    }
}