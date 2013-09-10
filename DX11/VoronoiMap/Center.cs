using System.Collections.Generic;
using SlimDX;

namespace VoronoiMap {
    public class Center {
        public int Index { get; set; }
        public Vector2 Point { get; set; }
        public bool Water { get; set; }
        public bool Ocean { get; set; }
        public bool Coast { get; set; }
        public bool Border { get; set; }
        public string Biome { get; set; }
        public float Elevation { get; set; }
        public float Moisture { get; set; }

        public List<Center> Neighbors { get; set; }
        public List<Edge> Borders { get; set; }
        public List<Corner> Corners { get; set; }
    }
}