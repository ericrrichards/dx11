using System;
using System.Drawing;
using SlimDX;

namespace Core.Terrain {
    /// <summary>
    /// A logical terrain tile in our world map
    /// </summary>
    public class MapTile : PriorityQueueNode {
        /// <summary>
        /// World-space height (y) of the tile center
        /// </summary>
        public float Height { get { return WorldPos.Y; } }
        /// <summary>
        /// World-space position of the tile center
        /// </summary>
        public Vector3 WorldPos { get; set; }
        /// <summary>
        /// 2D position of the tile in the terrain grid
        /// </summary>
        public Point MapPosition { get; set; }
        /// <summary>
        /// Type of the terrain tile - grass, hill, mountain, snow, etc
        /// </summary>
        public int Type { get; set; }
        /// <summary>
        /// Flag that determines if the tile is walkable
        /// </summary>
        public bool Walkable { get; set; }
        /// <summary>
        /// Tile set that the tile belongs to.  Paths can only be created between tiles in the same set
        /// </summary>
        public int Set { get; set; }
        /// <summary>
        /// Estimate of the cost to the goal tile, using A* pathfinding heuristic
        /// </summary>
        public float F { get; set; }
        /// <summary>
        /// Actual cost of reaching this tile from the start tile in the path
        /// </summary>
        public float G { get; set; }
        /// <summary>
        /// Previous tile in the computed path
        /// </summary>
        public MapTile Parent { get; set; }
        /// <summary>
        /// Connections to the neighboring tiles - square grid provides 8 directions of movement, up/down, left/right and diagonals
        /// </summary>
        public readonly MapEdge[] Edges = new MapEdge[8];

        public const float MaxSlope = 0.6f;
    }
    /// <summary>
    /// A connection between two adjacent terrain tiles
    /// </summary>
    public class MapEdge {
        /// <summary>
        /// Start tile
        /// </summary>
        public MapTile Node1 { get; private set; }
        /// <summary>
        /// End tile
        /// </summary>
        public MapTile Node2 { get; private set; }
        /// <summary>
        /// Cost of traversing this edge
        /// </summary>
        public float Cost { get; private set; }

        private MapEdge(MapTile n1, MapTile n2, float cost) {
            Node1 = n1;
            Node2 = n2;
            Cost = cost;
        }
        /// <summary>
        /// Calculate the cost to traverse this edge based on the slope between the two tile centers
        /// </summary>
        /// <param name="n1"></param>
        /// <param name="n2"></param>
        /// <returns></returns>
        private static float CalculateCost(MapTile n1, MapTile n2) {
            var dx = Math.Abs(n1.WorldPos.X - n2.WorldPos.X);
            var dz = Math.Abs(n1.WorldPos.Z - n2.WorldPos.Z);
            var dy = Math.Abs(n1.WorldPos.Y - n2.WorldPos.Y);

            var dxz = MathF.Sqrt(dx * dx + dz * dz);
            var slope = dy / dxz;

            return 1 +slope;
        }
        /// <summary>
        /// Factory method to create an edge between two terrain tiles
        /// If the slope between two tiles is too great, return null, indicating that there is no connection between tiles
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="neighbor"></param>
        /// <returns></returns>
        public static MapEdge Create(MapTile tile, MapTile neighbor) {
            var cost = CalculateCost(tile, neighbor);
            if (cost < 1+MapTile.MaxSlope) {
                return new MapEdge(tile, neighbor, cost);
            }
            return null;
        }
    }
}