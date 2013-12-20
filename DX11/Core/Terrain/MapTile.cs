using System;
using System.Drawing;
using SlimDX;

namespace Core.Terrain {
    public class MapTile : PriorityQueueNode {

        public float Height { get; set; }
        public Point MapPosition { get; set; }
        public int Type { get; set; }
        public bool Walkable { get; set; }
        public int Set { get; set; }
        public float F { get; set; }
        public float G { get; set; }
        public MapTile Parent { get; set; }
        public Vector3 WorldPos { get; set; }

        //public MapTile[] Neighbors = new MapTile[8];
        public MapEdge[] Edges = new MapEdge[8];

        
    }

    public class MapEdge {
        public MapTile Node1 { get; private set; }
        public MapTile Node2 { get; private set; }
        public float Cost { get; private set; }

        public MapEdge(MapTile n1, MapTile n2) {
            Node1 = n1;
            Node2 = n2;
            Cost = CalculateCost(n1, n2);
        }

        private MapEdge(MapTile n1, MapTile n2, float cost) {
            Node1 = n1;
            Node2 = n2;
            Cost = cost;
        }

        private static float CalculateCost(MapTile n1, MapTile n2) {
            var dx = Math.Abs(n1.WorldPos.X - n2.WorldPos.X);
            var dz = Math.Abs(n1.WorldPos.Z - n2.WorldPos.Z);
            var dy = Math.Abs(n1.WorldPos.Y - n2.WorldPos.Y);

            var dxz = MathF.Sqrt(dx * dx + dz * dz);
            var slope = dy / dxz;

            return slope;
        }

        public static MapEdge Create(MapTile tile, MapTile neighbor) {
            var cost = CalculateCost(tile, neighbor);
            if (cost < 0.6f) {
                return new MapEdge(tile, neighbor, cost);
            }
            return null;
        }
    }
}