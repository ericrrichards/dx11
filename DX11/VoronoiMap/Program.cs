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
        private Dictionary<int, List<Corner>> _cornerMap;

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

            Console.WriteLine("Build graph...");
            var voronoi = new Voronoi.Voronoi(_points, null, new Rectangle(0, 0, _size, _size));
            BuildGraph(_points, voronoi);
            InmproveCorners();
            _points = null;

            Console.WriteLine("Assign elevations");
            AssignCornerElevations();
            AssignOceanCoastAndLand();
            RedistributeElevations(LandCorners(_corners));

            foreach (var q in _corners) {
                if (q.Ocean || q.Coast) {
                    q.Elevation = 0.0f;
                }
            }
            AssignPolygonElevations();

            Console.WriteLine("Assign moisture...");
            CalculateDownslopes();
            CalculateWatersheds();
            CreateRivers();
            AssignCornerMoisture();
            RedistributeMoisture(LandCorners(_corners));
            AssignPolygonMoisture();

            Console.WriteLine("Decorate map...");
            AssignBiomes();

        }

        private void AssignBiomes() {
            foreach (var p in _centers) {
                p.Biome = GetBiome(p);
            }
        }

        private string GetBiome(Center p) {
            if (p.Ocean) {
                return "Ocean";
            }
            if (p.Water) {
                if (p.Elevation < 0.1f) return "Marsh";
                if (p.Elevation > 0.8f) return "Ice";
                return "Lake";
            }
            if (p.Coast) {
                return "Beach";
            }
            if (p.Elevation > 0.8f) {
                if (p.Moisture > 0.50f) return "Snow";
                if (p.Moisture > 0.33f) return "Tundra";
                if (p.Moisture > 0.16f) return "Bare";
                return "Scorched";
            }
            if (p.Elevation > 0.6f) {
                if (p.Moisture > 0.66f) return "Taiga";
                if (p.Moisture > 0.33f) return "Shrubland";
                return "TemperateDesert";
            }
            if (p.Elevation > 0.3f) {
                if (p.Moisture > 0.83) return "TemperateRainForest";
                if (p.Moisture > 0.50f) return "TemperateDecidousForest";
                if (p.Moisture > 0.16f) return "Grassland";
                return "TermperateDesert";
            }
            if (p.Moisture > 0.66f) return "TropicalRainForest";
            if (p.Moisture > 0.33f) return "TropicalSeasonalForest";
            if (p.Moisture > 0.16f) return "Grassland";
            return "SubtropicalDesert";
        }

        private void AssignPolygonMoisture() {
            foreach (var p in _centers) {
                var sumMoisture = 0.0f;
                foreach (var q in p.Corners) {
                    if (q.Moisture > 1.0f) q.Moisture = 1.0f;
                    sumMoisture += q.Moisture;
                }
                p.Moisture = sumMoisture/p.Corners.Count;
            }
        }

        private static void RedistributeMoisture(List<Corner> locations) {
            locations.Sort(new Corner.MoistureComparer());
            for (var i = 0; i < locations.Count; i++) {
                locations[i].Moisture = i/(locations.Count - 1.0f);
            }
        }

        private void AssignCornerMoisture() {
            var queue = new Queue<Corner>();
            foreach (var q in _corners) {
                if ((q.Water || q.River > 0) && !q.Ocean) {
                    q.Moisture = q.River > 0 ? Math.Min(3.0f, (0.2f*q.River)) : 1.0f;
                    queue.Enqueue(q);
                } else {
                    q.Moisture = 0.0f;
                }
            }
            while (queue.Count > 0) {
                var q = queue.Dequeue();
                foreach (var r in q.Adjacent) {
                    var newMoisture = q.Moisture*0.9f;
                    if (!(newMoisture > r.Moisture)) continue;
                    r.Moisture = newMoisture;
                    queue.Enqueue( r);
                }
            }
            foreach (var q in _corners.Where(q => q.Ocean || q.Coast)) {
                q.Moisture = 1.0f;
            }
        }

        private void CreateRivers() {
            for (var i = 0; i < _size/2; i++) {
                var q = _corners[_mapRandom.NextIntRange(0, _corners.Count - 1)];
                if ( q.Ocean || q.Elevation < 0.3f || q.Elevation > 0.9f) continue;
                while (!q.Coast) {
                    if (q == q.Downslope) {
                        break;
                    }
                    var edge = LookupEdgeFromCorner(q, q.Downslope);
                    edge.River = edge.River + 1;
                    q.River = (q.River | 0) + 1;
                    q.Downslope.River = (q.Downslope.River | 0) + 1;
                    q = q.Downslope;
                }
            }
            ;
        }

        private static Edge LookupEdgeFromCorner(Corner q, Corner s) {
            return q.Protrudes.FirstOrDefault(edge => edge.V0 == s || edge.V1 == s);
        }

        private void CalculateWatersheds() {
            foreach (var q in _corners) {
                q.Watershed = q;
                if (!q.Ocean && !q.Coast) {
                    q.Watershed = q.Downslope;
                }
            }
            for (var i = 0; i < 100; i++) {
                var changed = false;
                foreach (var q in _corners) {
                    if (q.Ocean || q.Coast || q.Watershed.Coast) continue;
                    var r = q.Downslope.Watershed;
                    if (!r.Ocean) q.Watershed = r;
                    changed = true;
                }
                if (!changed) break;
            }
            foreach (var r in _corners.Select(q => q.Watershed)) {
                r.WatershedSize = 1 + (r.WatershedSize | 0);
            }
        }

        private void CalculateDownslopes() {
            foreach (var q in _corners) {
                var r = q;
                foreach (var s in q.Adjacent) {
                    if (s.Elevation <= r.Elevation) {
                        r = s;
                    }
                }
                q.Downslope = r;
            }
        }

        private void AssignPolygonElevations() {
            foreach (var p in _centers) {
                var sumElevation = p.Corners.Sum(q => q.Elevation);
                p.Elevation = sumElevation/p.Corners.Count;
            }
        }

        private static void RedistributeElevations(List<Corner> locations) {
            const float scaleFactor = 1.1f;
            locations.Sort(new Corner.ElevationComparer());
            for (int i = 0; i < locations.Count; i++) {
                var y = i/(locations.Count - 1.0f);
                var x = (float)(Math.Sqrt(scaleFactor) - Math.Sqrt(scaleFactor*(1 - y)));
                if (x > 1.0f) x = 1.0f;
                locations[i].Elevation = x;
            }
        }

        private static List<Corner> LandCorners(IEnumerable<Corner> corners) {
            return corners.Where(q => !q.Ocean && !q.Coast).ToList();
        }

        private void AssignOceanCoastAndLand() {
            var queue = new Queue<Center>();
            foreach (var p in _centers) {
                var numWater = 0;
                foreach (var q in p.Corners) {
                    if (q.Border) {
                        p.Border = true;
                        p.Ocean = true;
                        q.Water = true;
                        queue.Enqueue(p);
                    }
                    if (q.Water) {
                        numWater++;
                    }
                }
                p.Water = (p.Ocean || numWater >= p.Corners.Count*LakeThreshold);
            }
            while (queue.Count > 0) {
                var p = queue.Dequeue();
                foreach (var r in p.Neighbors.Where(r => r.Water && !r.Ocean)) {
                    r.Ocean = true;
                    queue.Enqueue(r);
                }
            }
            foreach (var p in _centers) {
                var numOcean = 0;
                var numLand = 0;
                foreach (var r in p.Neighbors) {
                    if (r.Ocean) numOcean++;
                    if (!r.Water) numLand++;
                }
                p.Coast = (numOcean > 0) && (numLand > 0);
            }
            foreach (var q in _corners) {
                var numOcean = 0;
                var numLand = 0;
                foreach (var p in q.Touches) {
                    if (p.Ocean) numOcean++;
                    if (!p.Water) numLand++;
                }
                q.Ocean = (numOcean == q.Touches.Count);
                q.Coast = (numOcean > 0) && (numLand > 0);
                q.Water = q.Border || ((numLand != q.Touches.Count) && !q.Coast);
            }

        }

        private void AssignCornerElevations() {
            var queue = new Queue<Corner>();
            foreach (var q in _corners) {
                q.Water = !Inside(q.Point);
            }
            foreach (var q in _corners) {
                if (q.Border) {
                    q.Elevation = 0.0f;
                    queue.Enqueue(q);
                } else {
                    q.Elevation = float.PositiveInfinity;
                }
            }
            while (queue.Count > 0) {
                var q = queue.Dequeue();
                foreach ( var s in q.Adjacent) {
                    var newElevation = 0.01f + q.Elevation;
                    if (!q.Water && !s.Water) {
                        newElevation += 1;
                    }
                    if (newElevation < s.Elevation) {
                        s.Elevation = newElevation;
                        queue.Enqueue(s);
                    }
                }
            }
        }

        private bool Inside(Vector2 p) {
            return _islandShape(new Vector2(2*(p.X/_size - 0.5f), 2*(p.Y/_size - 0.5f)));
        }

        private void InmproveCorners() {
            var newCorners = Enumerable.Repeat(new Vector2(), _corners.Count).ToList();
            foreach (var q in _corners) {
                if (q.Border) {
                    newCorners[q.Index] = q.Point;
                } else {
                    var point = new Vector2();
                    foreach (var r in q.Touches) {
                        point.X += r.Point.X;
                        point.Y += r.Point.Y;
                    }
                    point.X /= q.Touches.Count;
                    point.Y /= q.Touches.Count;
                    newCorners[q.Index] = point;
                }
            }
            for (int i = 0; i < _corners.Count; i++) {
                _corners[i].Point = newCorners[i];
            }
            foreach (var edge in _edges) {
                if (edge.V0 != null && edge.V1 != null) {
                    edge.Midpoint = Vector2.Lerp(edge.V0.Point, edge.V1.Point, 0.5f);
                }
            }
        }

        private void BuildGraph(IEnumerable<Vector2> points, Voronoi.Voronoi voronoi) {
            var libEdges = voronoi.Edges;
            var centerLookup = new Dictionary<Vector2, Center>();

            foreach (var point in points) {
                var p = new Center {
                    Index = _centers.Count,
                    Point = point,
                    Neighbors = new List<Center>(),
                    Borders = new List<Edge>(),
                    Corners = new List<Corner>()
                };
                _centers.Add(p);
                centerLookup[point] = p;
            }
            foreach (var p in _centers) {
                voronoi.Region(p.Point);
            }
            _cornerMap = new Dictionary<int, List<Corner>>();

            foreach (var libEdge in libEdges) {
                var dedge = libEdge.DelaunayLine();
                var vedge = libEdge.VoronoiEdge();

                var edge = new Edge {
                    Index = _edges.Count,
                    River = 0,
                    Midpoint = vedge.P0.HasValue && vedge.P1.HasValue ? Vector2.Lerp(vedge.P0.Value, vedge.P1.Value, 0.5f) : (Vector2?) null
                };
                _edges.Add(edge);
                
                
                edge.V0 = MakeCorner(vedge.P0);
                edge.V1 = MakeCorner(vedge.P1);
                edge.D0 = dedge.P0.HasValue ? centerLookup[dedge.P0.Value] : null;
                edge.D1 = dedge.P1.HasValue ? centerLookup[dedge.P1.Value] : null;

                if (edge.D0 != null) {edge.D0.Borders.Add(edge);}
                if (edge.D1 != null) {  edge.D1.Borders.Add(edge);}
                if (edge.V0 != null) {  edge.V0.Protrudes.Add(edge);}
                if (edge.V1 != null) {  edge.V1.Protrudes.Add(edge);}

                if (edge.D0 != null && edge.D1 != null) {
                    AddToCenterList(edge.D0.Neighbors, edge.D1);
                    AddToCenterList(edge.D1.Neighbors, edge.D0);
                }
                if (edge.V0 != null && edge.V1 != null) {
                    AddToCornersList(edge.V0.Adjacent, edge.V1);
                    AddToCornersList(edge.V1.Adjacent, edge.V0);
                }
                if (edge.D0 != null) {
                    AddToCornersList(edge.D0.Corners, edge.V0);
                    AddToCornersList(edge.D0.Corners, edge.V1);
                }
                if (edge.D1 != null) {
                    AddToCornersList(edge.D1.Corners, edge.V0);
                    AddToCornersList(edge.D1.Corners, edge.V1);
                }

                if (edge.V0 != null) {
                    AddToCenterList(edge.V0.Touches, edge.D0);
                    AddToCenterList(edge.V0.Touches, edge.D1);
                }
                if (edge.V1 != null) {
                    AddToCenterList(edge.V1.Touches, edge.D0);
                    AddToCenterList(edge.V1.Touches, edge.D1);
                }
            }
        }

        private static void AddToCornersList(IList<Corner> v, Corner x) {
            if ( x != null && v.IndexOf(x) < 0) v.Add(x);
        }

        private static void AddToCenterList(IList<Center> neighbors, Center x) {
            if ( x!=null && neighbors.IndexOf(x) < 0) neighbors.Add(x);
        }

        private Corner MakeCorner(Vector2? p) {
            if (p == null) {
                return null;
            }
            var point = p.Value;
            int bucket;
           
            for (bucket = (int)point.X-1; bucket < (int)point.X + 1; bucket++) {
                if ( !_cornerMap.ContainsKey(bucket)) continue;
                foreach ( var q in _cornerMap[bucket]) {
                    var dx = point.X - q.Point.X;
                    var dy = point.Y - q.Point.Y;
                    if (dx*dx + dy*dy < 1e-6) {
                        return q;
                    }
                }
            }
            bucket = (int) point.X;
            if (_cornerMap[bucket] == null) {
                _cornerMap[bucket] = new List<Corner>();
            }
            var c = new Corner {
                Index = _corners.Count,
                Point = point,
                Border = (Equals(point.X, 0.0f) || Equals(point.X, _size) || Equals(point.Y, 0.0f) || Equals(point.Y, _size)),
                Touches = new List<Center>(),
                Protrudes = new List<Edge>(),
                Adjacent = new List<Corner>()
            };

            _corners.Add(c);
            _cornerMap[bucket].Add(c);
            return c;
        }

        private List<Vector2> GenerateRandomPoints() {
            var ret = new List<Vector2>();
            for (int i = 0; i < NumPoints; i++) {
                ret.Add(new Vector2(_mapRandom.NextFloatRange(10, _size-10), _mapRandom.NextFloatRange(10, _size-10)));
            }
            return ret;
        }

        private void ImproveRandomPoints(List<Vector2> points) {
            for (var i = 0; i < NumLloydIterations; i++) {
                var voronoi = new Voronoi.Voronoi(points, null, new Rectangle(0, 0, _size, _size));
                for (var index = 0; index < points.Count; index++) {
                    var p = points[index];
                    var region = voronoi.Region(p);
                    p.X = 0.0f;
                    p.Y = 0.0f;
                    foreach (var q in region) {
                        p.X += q.X;
                        p.Y += q.Y;

                    }
                    p.Y /= region.Count;
                    p.X /= region.Count;
                    points[index] = p;
                    region.Clear();
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
        public Vector2? Midpoint { get; set; }
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

    delegate bool IslandShape(Vector2 p);
}
