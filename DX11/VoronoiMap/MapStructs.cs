using System.Collections.Generic;
using System.Drawing;

namespace VoronoiMap {
    public class Center {
        public int Index { get; set; }
        public PointF Point { get; set; }
        public List<Center> Neighbors { get; set; }
        public List<Edge1> Borders { get; set; }
        public List<Corner> Corners { get; set; }
        public bool Border { get; set; }
        public bool Ocean { get; set; }
        public bool Water { get; set; }
        public bool Coast { get; set; }
        public float Elevation { get; set; }
        public float Moisture { get; set; }
        public Biomes Biome { get; set; }

        public Center() {
            Neighbors = new List<Center>();
            Borders = new List<Edge1>();
            Corners = new List<Corner>();
        }
    }

    public class Edge1 {
        public int Index { get; set; }
        public Center D0 { get; set; }
        public Center D1 { get; set; }
        public Corner V0 { get; set; }
        public Corner V1 { get; set; }
        public PointF Midpoint { get; set; }
        public int River { get; set; }
    }

    public class Corner {
        public int Index { get; set; }
        public PointF Point { get; set; }
        public List<Center> Touches { get; set; }
        public List<Edge1> Protrudes { get; set; }
        public List<Corner> Adjacent { get; set; }
        public bool Border { get; set; }
        public bool Water { get; set; }
        public float Elevation { get; set; }
        public bool Ocean { get; set; }
        public bool Coast { get; set; }
        public Corner Downslope { get; set; }
        public Corner Watershed { get; set; }
        public int WatershedSize { get; set; }
        public int River { get; set; }
        public float Moisture { get; set; }

        public Corner() {
            Touches = new List<Center>();
            Protrudes = new List<Edge1>();
            Adjacent = new List<Corner>();
        }
    }

    public enum Biomes {
        Ocean,
        Marsh,
        Ice,
        Lake,
        Beach,
        Snow,
        Tundra,
        Bare,
        Scorched,
        Taiga,
        Shrubland,
        TemperateDesert,
        TemperateRainForest,
        TemperateDecidousForest,
        Grassland,
        TropicalRainForest,
        TropicalSeasonalForest,
        SubtropicalDesert
    }
}
