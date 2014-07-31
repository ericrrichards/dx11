namespace Algorithms.Voronoi {
    public abstract class ICoord {
        public Point Coord { get; protected set; }
        public float X { get { return Coord.X; } }
        public float Y { get { return Coord.Y; } }

        public ICoord() {
            Coord = null;
        }

        public float Distance(ICoord p) {
            return Point.Distance(p.Coord, Coord);
        }
    }
}