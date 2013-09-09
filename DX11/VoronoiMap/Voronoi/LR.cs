namespace VoronoiMap.Voronoi {
    public class LR {
        public static readonly LR Left  = new LR("left");
        public static readonly LR Right = new LR("right");
        private readonly string _name;

        private LR(string name) {
            _name = name;
        }
        public static LR Other(LR leftRight) {
            return leftRight == Left ? Right : Left;
        }
        public override string ToString() {
            return _name;
        }
    }
}