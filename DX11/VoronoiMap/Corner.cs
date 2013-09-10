using System.Collections.Generic;
using SlimDX;

namespace VoronoiMap {
    public class Corner {
        public int Index { get; set; }
        public Vector2 Point { get; set; }
        public bool Water { get; set; }
        public bool Ocean { get; set; }
        public bool Coast { get; set; }
        public bool Border { get; set; }
        public string Biome { get; set; }
        public float Elevation { get; set; }
        public float Moisture { get; set; }

        public List<Center> Touches { get; set; }
        public List<Edge> Protrudes { get; set; }
        public List<Corner> Adjacent { get; set; }

        public int River { get; set; }
        public Corner Downslope { get; set; }
        public Corner Watershed { get; set; }
        public int WatershedSize { get; set; }

        public class ElevationComparer : IComparer<Corner> {
            public int Compare(Corner x, Corner y) {
                return x.Elevation.CompareTo(y.Elevation);
            }
        }

        public class MoistureComparer : IComparer<Corner> {
            public int Compare(Corner x, Corner y) {
                return x.Moisture.CompareTo(y.Moisture);
            }
        }
    }
}