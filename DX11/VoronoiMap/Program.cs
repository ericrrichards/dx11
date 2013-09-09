using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlimDX;

namespace VoronoiMap {
    class Program {
        static void Main(string[] args) {
        }
    }

    
    public class Map {
        public const int NumPoints = 2000;
        public const float LakeThreshold = 0.3f;
        public const int NumLloydIterations = 2;

        private float _size;
        private IslandShape _islandShape;
        

        private ParkerMillerPnrg _mapRandom;

        private List<Vector2> _points;
        private List<Center> _centers;
        private List<Corner> _corners;
        private List<Edge> _edges;

        public Map(float size) {
            _size = size;
            Reset();
        }
        public void NewIsland(string type, int seed, int variant) {
            _mapRandom.Seed = variant;
        }

        public void Reset() {
            _points.Clear();
            _edges.Clear();
            _centers.Clear();
            _corners.Clear();

        }
        public void Go(int first, int last) {
            Console.WriteLine("Place points...");
            Reset();
            _points = GenerateRandomPoints();

            Console.WriteLine("Improve points...");
            ImproveRandomPoints(_points);

        }

        private List<Vector2> GenerateRandomPoints() {
            var ret = new List<Vector2>();
            for (int i = 0; i < NumPoints; i++) {
                ret.Add(new Vector2(_mapRandom.NextFloatRange(10, _size-10), _mapRandom.NextFloatRange(10, _size-10)));
            }
            return ret;
        }

        private void ImproveRandomPoints(List<Vector2> points) {
            for (int i = 0; i < NumLloydIterations; i++) {
                var voronoi = new Voronoi.Voronoi(points, null, new Rectangle(0, 0, _size, _size));
                foreach (var p in points) {
                    var region = voronoi.Region(p);
                }
            }
        }
    }

    class Edge {
        public int Index { get; set; }
        public Center D0 { get; set; }
        public Center D1 { get; set; }
        public Corner V0 { get; set; }
        public Corner V1 { get; set; }
        public Vector2 Midpoint { get; set; }
        public int River { get; set; }
    }

    class Corner {
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
    }

    class Center {
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

    class IslandShape {
        
    }
}
