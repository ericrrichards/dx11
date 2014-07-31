using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using Core;
using SlimDX.Direct3D9;

namespace VoronoiMap {

    public class VoronoiMap {
        private readonly VoronoiGraph _graph;
        private List<Center> Centers { get; set; }
        private List<Corner> Corners { get; set; }
        private List<Edge1> Edges { get; set; }
        private const float LakeThreshold = 0.3f;

        private static readonly Dictionary<string, SolidBrush> DisplayBrushes = new Dictionary<string, SolidBrush>() {
            {"Ocean", new SolidBrush(Color.FromArgb(0x44,0x44,0x7a))},
            {"Coast", new SolidBrush(Color.FromArgb(0x33,0x33,0x5a))},
            {"LakeShore",new SolidBrush(Color.FromArgb(0x22,0x55,0x88))},
            {"Lake",new SolidBrush(Color.FromArgb(0x33,0x66,0x99))},
            {"River", new SolidBrush(Color.FromArgb(0x22,0x55,0x88))},
            {"Marsh",new SolidBrush(Color.FromArgb(0x2f,0x66,0x66))},
            {"Ice", new SolidBrush(Color.FromArgb(0x99,0xff,0xff))},
            {"Beach",new SolidBrush(Color.FromArgb(0xa0,0x90,0x77))},
            {"Road1",new SolidBrush(Color.FromArgb(0x44,0x22,0x11))},
            {"Road2",new SolidBrush(Color.FromArgb(0x55,0x33,0x22))},
            {"Road3",new SolidBrush(Color.FromArgb(0x66,0x44,0x33))},
            {"Bridge",new SolidBrush(Color.FromArgb(0x68,0x68,0x60))},
            {"Lava",new SolidBrush(Color.FromArgb(0xcc,0x33,0x33))},
                
            {"Snow",new SolidBrush(Color.FromArgb(0xff,0xff,0xff))},
            {"Tundra",new SolidBrush(Color.FromArgb(0xbb,0xbb,0xaa))},
            {"Bare",new SolidBrush(Color.FromArgb(0x88,0x88,0x88))},
            {"Scorched",new SolidBrush(Color.FromArgb(0x55,0x55,0x55))},
            {"Taiga",new SolidBrush(Color.FromArgb(0x99,0xaa,0x77))},
            {"Shrubland",new SolidBrush(Color.FromArgb(0x88,0x99,0x77))},
            {"TemperateDesert",new SolidBrush(Color.FromArgb(0xc9,0xd2,0x9b))},
            {"TemperateRainForest",new SolidBrush(Color.FromArgb(0x44,0x88,0x55))},
            {"TemperateDecidousForest",new SolidBrush(Color.FromArgb(0x67,0x94,0x59))},
            {"Grassland",new SolidBrush(Color.FromArgb(0x88,0xaa,0x55))},
            {"SubtropicalDesert",new SolidBrush(Color.FromArgb(0xd2,0xb9,0x8b))},
            {"TropicalRainForest",new SolidBrush(Color.FromArgb(0x33,0x77,0x55))},
            {"TropicalSeasonalForest",new SolidBrush(Color.FromArgb(0x55,0x99,0x44))}
        };


        public VoronoiMap(VoronoiGraph g) {
            _graph = g;
            Reset();
            BuildGraph(g);
            //ImproveCorners();

            AssignCornerElevations();
            AssignOceanCoastAndLand();
            //RedistributeElevations(LandCorners(Corners));
            //AssignOceanElevations();
            //AssignPolygonElevations();

            //CalculateDownslopes();
            //CalculateWatersheds();
            //CreateRivers();
            //AssignCornerMoisture();
            //RedistributeMoisture(LandCorners(Corners));
            //AssignPolygonMoisture();

            //AssignBiomes();
        }

        private void AssignBiomes() {
            foreach (var p in Centers) {
                p.Biome = GetBiome(p);
            }
        }

        private Biomes GetBiome(Center p) {
            if (p.Ocean) { return Biomes.Ocean; }
            if (p.Water) {
                if (p.Elevation < 0.1) { return Biomes.Marsh; }
                if (p.Elevation > 0.8) { return Biomes.Ice; }
                return Biomes.Lake;
            }
            if (p.Coast) { return Biomes.Beach; }
            if (p.Elevation > 0.8) {
                if (p.Moisture > 0.8) { return Biomes.Snow; }
                if (p.Moisture > 0.5) { return Biomes.Snow; }
                if (p.Moisture > 0.33) { return Biomes.Tundra; }
                if (p.Moisture > 0.16) { return Biomes.Bare; }
                return Biomes.Scorched;
            }
            if (p.Elevation > 0.6) {
                if (p.Moisture > 0.66) return Biomes.Taiga;
                if (p.Moisture > 0.33) return Biomes.Shrubland;
                return Biomes.TemperateDesert;
            }
            if (p.Elevation > 0.3) {
                if (p.Moisture > 0.83) return Biomes.TemperateRainForest;
                if (p.Moisture > 0.5) return Biomes.TemperateDecidousForest;
                if (p.Moisture > 0.16) return Biomes.Grassland;
                return Biomes.TemperateDesert;
            }
            if (p.Moisture > 0.66) return Biomes.TropicalRainForest;
            if (p.Moisture > 0.33) return Biomes.TropicalSeasonalForest;
            if (p.Moisture > 0.16) return Biomes.Grassland;
            return Biomes.SubtropicalDesert;
        }

        private void AssignPolygonMoisture() {
            foreach (var p in Centers) {
                var sumMoisture = 0.0f;
                foreach (var q in p.Corners) {
                    if (q.Moisture > 1.0f) {
                        q.Moisture = 1.0f;
                    }
                    sumMoisture += q.Moisture;
                }
                p.Moisture = sumMoisture / p.Corners.Count;
            }
        }

        private void RedistributeMoisture(IEnumerable<Corner> locations) {
            locations = locations.OrderBy(l => l.Moisture);
            var i = 0.0f;
            foreach (var location in locations) {
                location.Moisture = i / locations.Count();
                i++;
            }
        }

        private void AssignCornerMoisture() {
            var queue = new Queue<Corner>();
            foreach (var q in Corners) {
                if ((q.Water || q.River > 0) && !q.Ocean) {
                    q.Moisture = q.River > 0 ? Math.Min(3.0f, (0.2f * q.River)) : 1.0f;
                    queue.Enqueue(q);
                } else {
                    q.Moisture = 0;
                }
            }
            while (queue.Any()) {
                var q = queue.Dequeue();
                foreach (var r in q.Adjacent) {
                    var newMoisture = q.Moisture * 0.9f;
                    if (newMoisture > r.Moisture) {
                        r.Moisture = newMoisture;
                        queue.Enqueue(r);
                    }
                }
            }
            foreach (var q in Corners) {
                if (q.Ocean || q.Coast) {
                    q.Moisture = 1.0f;
                }
            }
        }

        private void CreateRivers() {
            for (int i = 0; i < _graph.Sites.Count / 2; i++) {
                var q = Corners[MathF.Rand(Corners.Count - 1)];
                if (q.Ocean || q.Elevation < 0.3 || q.Elevation > 0.9) { continue; }

                while (!q.Coast) {
                    if (q == q.Downslope) {
                        break;
                    }
                    var edge = LookupEdgeFromCorner(q, q.Downslope);
                    edge.River = edge.River + 1;
                    q.River = q.River + 1;
                    q.Downslope.River = q.Downslope.River + 1;
                    q = q.Downslope;
                }
            }
        }

        private Edge1 LookupEdgeFromCorner(Corner q, Corner s) {
            foreach (var edge in q.Protrudes) {
                if (edge.V0 == s || edge.V1 == s) return edge;
            }
            return null;
        }

        private Edge1 LookupEdgeFromCenter(Center p, Center r) {
            foreach (var edge in p.Borders) {
                if (edge.D0 == r || edge.D1 == r) {
                    return edge;
                }
            }
            return null;
        }

        private void CalculateWatersheds() {
            foreach (var q in Corners) {
                q.Watershed = q;
                if (!q.Ocean && !q.Coast) {
                    q.Watershed = q.Downslope;
                }
            }
            for (int i = 0; i < 100; i++) {
                var changed = false;
                foreach (var q in Corners) {
                    if (!q.Ocean && !q.Coast && !q.Watershed.Coast) {
                        var r = q.Downslope.Watershed;
                        if (!r.Ocean) { q.Watershed = r; }
                        changed = true;
                    }
                }
                if (changed) { break; }
            }
            foreach (var q in Corners) {
                var r = q.Watershed;
                r.WatershedSize = 1 + (r.WatershedSize);
            }
        }

        private void CalculateDownslopes() {
            foreach (var q in Corners) {
                var r = q;
                foreach (var s in q.Adjacent) {
                    if (s.Elevation <= r.Elevation) {
                        r = s;
                    }
                }
                q.Downslope = r;
            }
        }

        private void AssignOceanElevations() {
            foreach (var q in Corners) {
                if (q.Ocean || q.Coast) {
                    q.Elevation = 0.0f;
                }
            }
        }

        private void AssignPolygonElevations() {
            foreach (var p in Centers) {
                var sumElevation = 0.0f;
                foreach (var q in p.Corners) {
                    sumElevation += q.Elevation;
                }
                p.Elevation = sumElevation / p.Corners.Count;
            }
        }

        private void RedistributeElevations(IEnumerable<Corner> locations) {
            const float scaleFactor = 1.1f;
            locations = locations.OrderBy(l => l.Elevation);
            var i = 0;
            foreach (var location in locations) {
                var y = i / (locations.Count() - 1);
                var x = MathF.Sqrt(scaleFactor) - MathF.Sqrt(scaleFactor * (1 - y));
                if (x > 1.0f) x = 1.0f;
                Corners.First(c => c.Index == location.Index).Elevation = x;
                i++;
            }
        }

        private IEnumerable<Corner> LandCorners(IEnumerable<Corner> corners) {
            return corners.Where(q => !q.Ocean && !q.Coast);
        }

        private void AssignOceanCoastAndLand() {
            var queue = new Queue<Center>();
            foreach (var p in Centers) {
                var numWater = 0;
                foreach (var q in p.Corners) {
                    if (q.Border) {
                        p.Border = true;
                        p.Ocean = true;
                        q.Water = true;
                        queue.Enqueue(p);
                    }
                    if (q.Water) {
                        numWater += 1;
                    }
                }
                p.Water = (p.Ocean || numWater >= p.Corners.Count * LakeThreshold);
            }
            while (queue.Any()) {
                var p = queue.Dequeue();
                foreach (var r in p.Neighbors) {
                    if (r.Water && !r.Ocean) {
                        r.Ocean = true;
                        queue.Enqueue(r);
                    }
                }
            }
            foreach (var p in Centers) {
                var numOcean = 0;
                var numLand = 0;
                foreach (var r in p.Neighbors) {
                    if (r.Ocean) numOcean++;
                    if (!r.Water) numLand++;
                }
                p.Coast = (numOcean > 0) && (numLand > 0);
            }
            foreach (var q in Corners) {
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

            foreach (var q in Corners) {
                q.Water = !Inside(q.Point);
            }
            foreach (var q in Corners) {
                if (q.Border) {
                    q.Elevation = 0.0f;
                    queue.Enqueue(q);
                } else {
                    q.Elevation = float.MaxValue;
                }
            }

            while (queue.Any()) {
                var q = queue.Dequeue();
                foreach (var s in q.Adjacent) {
                    var newElevation = 0.01f + q.Elevation;
                    if (!q.Water && !s.Water) {
                        newElevation += 1;
                        // hack
                        //newElevation += MathF.Rand(0, 1);
                    }
                    if (newElevation < s.Elevation) {
                        s.Elevation = newElevation;
                        queue.Enqueue(s);
                    }
                }
            }

        }

        private bool Inside(PointF point) {
#warning do this eventually
            return true;
        }


        private void Reset() {
            Centers = new List<Center>();
            Corners = new List<Corner>();
            Edges = new List<Edge1>();
        }

        private void ImproveCorners() {
            var newCorners = new PointF[Corners.Count];

            foreach (var q in Corners) {
                if (q.Border) {
                    newCorners[q.Index] = q.Point;
                } else {
                    var point = new PointF();
                    foreach (var r in q.Touches) {
                        point.X += r.Point.X;
                        point.Y += r.Point.Y;
                    }
                    point.X /= q.Touches.Count;
                    point.Y /= q.Touches.Count;
                    newCorners[q.Index] = point;
                }
            }
            for (int i = 0; i < Corners.Count; i++) {
                Corners[i].Point = newCorners[i];
            }
            foreach (var edge in Edges) {
                if (edge.V0 != null && edge.V1 != null) {
                    edge.Midpoint = new PointF((edge.V0.Point.X + edge.V1.Point.X) * 0.5f, (edge.V0.Point.Y + edge.V1.Point.Y) * 0.5f);
                }
            }
        }


        private void BuildGraph(VoronoiGraph voronoi) {
            var points = voronoi.Sites;
            var libedges = voronoi.Edges;
            var centerLookup = new Dictionary<PointF, Center>();

            foreach (var point in points) {
                point.Region(new RectangleF(0, 0, voronoi.Width, voronoi.Height));
                var p = new Center {
                    Index = Centers.Count,
                    Point = point
                };
                Centers.Add(p);
                centerLookup[point] = p;
                
            }

            foreach (var libedge in libedges.Where(e=>e.Visible)) {
                var dedge = libedge.DelauneyLine;
                var vedge = libedge.VoronoiEdge;

                var edge = new Edge1 {
                    Index = Edges.Count,
                    Midpoint = (Valid(vedge.P1) && Valid(vedge.P2)) ? new PointF((vedge.P1.X + vedge.P2.X) * 0.5f, (vedge.P1.Y + vedge.P2.Y) * 0.5f) : InvalidPoint
                };

                Edges.Add(edge);

                // edges point to corners
                edge.V0 = MakeCorner(vedge.P1);
                edge.V1 = MakeCorner(vedge.P2);
                // edges point to centers
                edge.D0 = centerLookup[dedge.P1];
                edge.D1 = centerLookup[dedge.P2];

                // Centers point to edges
                if (edge.D0 != null) { edge.D0.Borders.Add(edge); }
                if (edge.D1 != null) { edge.D1.Borders.Add(edge); }
                // Corners point to edges
                if (edge.V0 != null) { edge.V0.Protrudes.Add(edge); }
                if (edge.V1 != null) { edge.V1.Protrudes.Add(edge); }

                // Centers point to centers
                if (edge.D0 != null && edge.D1 != null) {
                    AddToCenterList(edge.D0.Neighbors, edge.D1);
                    AddToCenterList(edge.D1.Neighbors, edge.D0);
                }
                // Corners point to corners
                if (edge.V0 != null && edge.V1 != null) {
                    AddToCornerList(edge.V0.Adjacent, edge.V1);
                    AddToCornerList(edge.V1.Adjacent, edge.V0);
                }
                // Centers point to corners
                if (edge.D0 != null) {
                    AddToCornerList(edge.D0.Corners, edge.V0);
                    AddToCornerList(edge.D0.Corners, edge.V1);
                }
                if (edge.D1 != null) {
                    AddToCornerList(edge.D1.Corners, edge.V0);
                    AddToCornerList(edge.D1.Corners, edge.V1);
                }

                // Corners point to centers
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

        private static void AddToCenterList(ICollection<Center> v, Center center) {
            if (center != null && !v.Contains(center)) {
                v.Add(center);
            }
        }
        private static void AddToCornerList(ICollection<Corner> v, Corner corner) {
            if (corner != null && !v.Contains(corner)) {
                v.Add(corner);
            }
        }

        private static readonly PointF InvalidPoint = new PointF(-1, -1);
        private static bool Valid(PointF p1) { return p1 != InvalidPoint; }

        private readonly Dictionary<int, List<Corner>> _cornerMap = new Dictionary<int, List<Corner>>();
        private Corner MakeCorner(PointF p) {
            if (p == InvalidPoint) return null;
            int bucket;
            for (bucket = (int)p.X - 1; bucket < (int)p.X + 1; bucket++) {
                if (!_cornerMap.ContainsKey(bucket)) {
                    continue;
                }
                foreach (var q in _cornerMap[bucket]) {
                    var dx = p.X - q.Point.X;
                    var dy = p.Y - q.Point.Y;
                    if (dx * dx + dy * dy < 1e-6) {
                        return q;
                    }
                }
            }
            bucket = (int)p.X;
            if (!_cornerMap.ContainsKey(bucket)) {
                _cornerMap[bucket] = new List<Corner>();
            }
            var c = new Corner {
                Index = Corners.Count,
                Point = p,
                Border = (p.X <= 0 || p.X >= _graph.Width || p.Y <= 0 || p.Y >= _graph.Height)
            };
            Corners.Add(c);
            _cornerMap[bucket].Add(c);
            return c;

        }


        public void RenderPolygons(Graphics g) {
            g.Clear(DisplayBrushes["Ocean"].Color);

            foreach (var p in Centers) {
                var c = DisplayBrushes["Ocean"].Color;
                var shape = new GraphicsPath();
                foreach (var edge in p.Borders) {
                    if (edge.V0 != null && edge.V1 != null) {
                        if (edge.River > 0) {
                            g.DrawLine(new Pen(DisplayBrushes["River"].Color, 2), edge.V0.Point, edge.V1.Point);
                        } else {
                            g.DrawLine(new Pen(Color.White), edge.V0.Point, edge.V1.Point);
                        }
                        shape.AddPolygon(new[] { p.Point, edge.V0.Point, edge.V1.Point });

                    }
                }
                if (p.Elevation > 0) {
                    c = Color.Lime;
                    var c2 = Color.Red;
                    c = InterpolateColor(c, c2, p.Elevation);
                }

                g.FillPath(new SolidBrush(c), shape);

                g.FillEllipse(new SolidBrush(Color.White), p.Point.X - 1.3f, p.Point.Y - 1.3f, 2.6f, 2.6f);

                foreach (var q in p.Corners) {
                    g.FillRectangle(new SolidBrush(Color.FromArgb(0x009900)), q.Point.X - 0.7f, q.Point.Y - 0.7f, 1.5f, 1.5f);
                }
            }
            
        }

        public Color InterpolateColor(Color c1, Color c2, float f) {
            var r = c2.R*f + c1.R*(1.0f - f);
            var g = c2.G*f + c1.G*(1.0f - f);
            var b = c2.B*f + c1.B*(1.0f - f);

            return Color.FromArgb((byte)r, (byte)g, (byte)b);
        }
    }


}
