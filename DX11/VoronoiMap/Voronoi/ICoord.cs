using SlimDX;

namespace VoronoiMap.Voronoi {
    public abstract class ICoord {
        public Vector2 Coord { get; set; }
        public float X { get { return Coord.X; } }
        public float Y { get { return Coord.Y; } }
    }
}