using System.Drawing;
using SlimDX;

namespace Core.Terrain {
    public class MapTile {

        public float Height { get; set; }
        public Point MapPosition { get; set; }
        public int Type { get; set; }
        public float Cost { get; set; }
        public bool Walkable { get; set; }
        public int Set { get; set; }
        public float F { get; set; }
        public float G { get; set; }
        public bool Open { get; set; }
        public bool Closed { get; set; }
        public MapTile Parent { get; set; }
        public Vector3 WorldPos { get; set; }

        public MapTile[] Neighbors = new MapTile[8];
    }
}