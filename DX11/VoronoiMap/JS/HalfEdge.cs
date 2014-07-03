namespace Fortune.FromJS {
    public class HalfEdge {
        public Edge Edge { get; set; }
        public Side Side { get; set; }
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
    }
}