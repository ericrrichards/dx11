using SlimDX;

namespace VoronoiMap {
    public class Edge {
        public int Index { get; set; }
        public Center D0 { get; set; }
        public Center D1 { get; set; }
        public Corner V0 { get; set; }
        public Corner V1 { get; set; }
        public Vector2? Midpoint { get; set; }
        public int River { get; set; }
    }
}