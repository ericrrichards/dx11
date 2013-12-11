namespace Core.Terrain {
    #region

    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;

    using SlimDX;
    using SlimDX.Direct3D11;

    using Device = SlimDX.Direct3D11.Device;

    #endregion

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

        public MapTile[] Neighbors = new MapTile[8];
    }


    public class Terrain : DisposableClass {
        public const int CellsPerPatch = 64;
        private const int TileSize = 2;
        private bool _disposed;
        private HeightMap _heightMap;
        private MapTile[] _tiles;

        private QuadTree _quadTree;

        private TerrainRenderer _renderer;
        public Matrix World { get; set; }

        public Image HeightMapImg { get { return _heightMap.Bitmap; } }

        public float Width { get { return (Info.HeightMapWidth - 1) * Info.CellSpacing; } }

        public float Depth { get { return (Info.HeightMapHeight - 1) * Info.CellSpacing; } }

        public TerrainRenderer Renderer { get { return _renderer; } }

        public InitInfo Info { get; private set; }

        public HeightMap HeightMap { get { return _heightMap; } }

        public QuadTree QuadTree { get { return _quadTree; } }

        public Terrain() {
            World = Matrix.Identity;

            _renderer = new TerrainRenderer(new Material { Ambient = Color.White, Diffuse = Color.White, Specular = new Color4(64.0f, 0, 0, 0), Reflect = Color.Black }, this);
        }

        

        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Util.ReleaseCom(ref _renderer);
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        public float Height(float x, float z) {
            var c = (x + 0.5f * Width) / Info.CellSpacing;
            var d = (z - 0.5f * Depth) / -Info.CellSpacing;
            var row = (int)Math.Floor(d);
            var col = (int)Math.Floor(c);

            var h00 = _heightMap[row, col];
            var h01 = _heightMap[row, col + 1];
            var h10 = _heightMap[(row + 1), col];
            var h11 = _heightMap[(row + 1), col + 1];

            var s = c - col;
            var t = d - row;

            if (s + t <= 1.0f) {
                var uy = h01 - h00;
                var vy = h01 - h11;
                return h00 + (1.0f - s) * uy + (1.0f - t) * vy;
            } else {
                var uy = h10 - h11;
                var vy = h01 - h11;
                return h11 + (1.0f - s) * uy + (1.0f - t) * vy;
            }
        }

        public void Init(Device device, DeviceContext dc, InitInfo info) {
            D3DApp.GD3DApp.ProgressUpdate.Draw(0, "Initializing terrain");

            Info = info;
            _heightMap = new HeightMap(Info.HeightMapWidth, Info.HeightMapHeight, Info.HeightScale);
            if (!string.IsNullOrEmpty(Info.HeightMapFilename)) {
                D3DApp.GD3DApp.ProgressUpdate.Draw(0.1f, "Loading terrain from file");
                _heightMap.LoadHeightmap(Info.HeightMapFilename);
            } else {
                D3DApp.GD3DApp.ProgressUpdate.Draw(0.1f, "Generating random terrain");
                GenerateRandomTerrain();
                D3DApp.GD3DApp.ProgressUpdate.Draw(0.50f, "Smoothing terrain");
                _heightMap.Smooth(true);
            }
            InitPathfinding();
            D3DApp.GD3DApp.ProgressUpdate.Draw(0.55f, "Building picking quadtree...");
            _quadTree = new QuadTree { Root = BuildQuadTree(new Vector2(0, 0), new Vector2((Info.HeightMapWidth - 1), (Info.HeightMapHeight - 1))) };

            
            

            Renderer.Init(device, dc, this);
        }

        private void InitPathfinding() {
            _tiles = new MapTile[Info.HeightMapWidth / TileSize * Info.HeightMapHeight / TileSize];
            for (var i = 0; i < _tiles.Length; i++) {
                _tiles[i] = new MapTile();
            }

            for (var y = 0; y < Info.HeightMapWidth / TileSize; y++) {
                for (var x = 0; x < Info.HeightMapHeight/TileSize; x++) {
                    var tile = GetTile(x, y) ;
                    var worldX = x * Info.CellSpacing - Width / 2;
                    var worldZ = -y * Info.CellSpacing + Depth / 2;
                    tile.Height = Height(worldX, worldZ);
                    tile.MapPosition = new Point(x, y);
                    

                    if (tile.Height > _heightMap.MaxHeight * (0.05f)) {
                        tile.Type = 0;
                    } else if (tile.Height > _heightMap.MaxHeight * (0.4f)) {
                        tile.Type = 1;
                    } else if (tile.Height > _heightMap.MaxHeight * (0.75f)) {
                        tile.Type = 2;
                    }
                }
            }
            for (var y = 0; y < Info.HeightMapWidth / TileSize; y++) {
                for (var x = 0; x < Info.HeightMapHeight / TileSize; x++) {
                    var tile = GetTile(x, y);

                    if (tile == null) {
                        continue;
                    }
                    var p = new[] {
                        new Point(x - 1, y - 1), new Point(x , y - 1), new Point(x + 1, y - 1),
                        new Point(x - 1, y),new Point(x + 1, y ),
                        new Point(x - 1, y + 1),new Point(x , y + 1),new Point(x + 1, y + 1)
                    };
                    var variance = 0.0f;
                    var nr = 0;
                    foreach (var point in p) {
                        if (!Within(point)) {
                            continue;
                        }
                        var neighbor = GetTile(point);
                        if (neighbor == null) {
                            continue;
                        }
                        var v = neighbor.Height - tile.Height;
                        variance += v * v;
                        nr++;
                    }
                    variance /= nr;
                    tile.Cost = variance + 0.1f;
                    if (tile.Cost > 1.0f)
                        tile.Cost = 1.0f;
                    tile.Walkable = tile.Cost < 0.5f;
                }
            }
            for (var y = 0; y < Info.HeightMapWidth / TileSize; y++) {
                for (var x = 0; x < Info.HeightMapHeight / TileSize; x++) {
                    var tile = GetTile(x, y);
                    if (tile != null && tile.Walkable) {
                        for (var i = 0; i < 8; i++) {
                            tile.Neighbors[i] = null;
                        }
                        var p = new[] {
                            new Point(x - 1, y - 1), new Point(x , y - 1), new Point(x + 1, y - 1),
                            new Point(x - 1, y),new Point(x + 1, y ),
                            new Point(x - 1, y + 1),new Point(x , y + 1),new Point(x + 1, y + 1)
                        };
                        for (var i = 0; i < 8; i++) {
                            if (!Within(p[i])) {
                                continue;
                            }
                            var neighbor = GetTile(p[i]);
                            if (neighbor != null && neighbor.Walkable) {
                                tile.Neighbors[i] = neighbor;
                            }
                        }
                    }
                }
            }
            CreateTileSets();
        }

        private void CreateTileSets() {
            var setNo = 0;
            for (var y = 0; y < Info.HeightMapWidth / TileSize; y++) {
                for (var x = 0; x < Info.HeightMapHeight / TileSize; x++) {
                    var tile = GetTile(x, y);
                    tile.Set = setNo++;
                }
            }
            var changed = true;
            while (changed) {
                changed = false;
                for (var y = 0; y < Info.HeightMapWidth / TileSize; y++) {
                    for (var x = 0; x < Info.HeightMapHeight / TileSize; x++) {
                        var tile = GetTile(x, y);
                        if (tile == null || !tile.Walkable) {
                            continue;
                        }
                        foreach (var neighbor in tile.Neighbors) {
                            if (neighbor == null || !neighbor.Walkable || neighbor.Set >= tile.Set) {
                                continue;
                            }
                            changed = true;
                            tile.Set = neighbor.Set;
                        }
                    }
                }
            }
        }

        public List<Point> GetPath(Point start, Point goal) {
            var startTile = GetTile(start);
            var goalTile = GetTile(goal);

            if (!Within(start) || !Within(goal) || start == goal || startTile == null || goalTile == null) {
                return new List<Point>();
            }
            if (!startTile.Walkable || !goalTile.Walkable || startTile.Set != goalTile.Set) {
                return new List<Point>();
            }
            var numTiles = Info.HeightMapWidth / TileSize * Info.HeightMapHeight / TileSize;
            for (var i = 0; i < numTiles; i++) {
                _tiles[i].F = _tiles[i].G = float.MaxValue;
                _tiles[i].Open = _tiles[i].Closed = false;
            }

            var open = new List<MapTile>();
            startTile.G = 0;
            startTile.F = H(start, goal);
            startTile.Open = true;
            open.Add(startTile);

            while (open.Any()) {
                var best = open.First();
                var bestPlace = 0;
                for (var i = 0; i < open.Count; i++) {
                    if (open[i].F < best.F) {
                        best = open[i];
                        bestPlace = i;
                    }
                }
                if (best == null)
                    break;

                open[bestPlace].Open = false;
                open.RemoveAt(bestPlace);
                if (best.MapPosition == goal) {
                    var p = new List<Point>();
                    var point = best;
                    while (point.MapPosition != start) {
                        p.Add(point.MapPosition);
                        point = point.Parent;
                    }
                    p.Reverse();
                    return p;
                }
                for (var i = 0; i < 8; i++) {
                    if (best.Neighbors[i] == null) {
                        continue;
                    }
                    var inList = false;
                    var newG = best.G + 1.0f;
                    var d = H(best.MapPosition, best.Neighbors[i].MapPosition);
                    var newF = newG + H(best.Neighbors[i].MapPosition, goal) + best.Neighbors[i].Cost * 5.0f * d;

                    if (best.Neighbors[i].Open || best.Neighbors[i].Closed) {
                        if (newF < best.Neighbors[i].F) {
                            best.Neighbors[i].G = newG;
                            best.Neighbors[i].F = newF;
                            best.Neighbors[i].Parent = best;
                        }
                        inList = true;
                    }
                    if (inList) {
                        continue;
                    }
                    best.Neighbors[i].F = newF;
                    best.Neighbors[i].G = newG;
                    best.Neighbors[i].Parent = best;
                    best.Neighbors[i].Open = true;
                    open.Add(best.Neighbors[i]);
                }
                best.Closed = true;
            }
            return new List<Point>();
        }

        private static float H(Point start, Point goal) { return MathF.Sqrt((goal.X - start.X) * (goal.X - start.X) + (goal.Y - start.Y) * (goal.Y - start.Y)); }

        private bool Within(Point p) {
            return p.X >= 0 && p.X < Info.HeightMapWidth / TileSize && p.Y >= 0 && p.Y < Info.HeightMapHeight / TileSize;
        }

        private MapTile GetTile(Point point) { return GetTile(point.X, point.Y); }

        private MapTile GetTile(int x, int y) {
            if (_tiles == null)
                return null;
            return _tiles[x + y * Info.HeightMapHeight / TileSize];
        }

        private QuadTreeNode BuildQuadTree(Vector2 topLeft, Vector2 bottomRight) {
            const float tolerance = 0.01f;

            // search the heightmap in order to get the y-extents of the terrain region
            var minMaxY = GetMinMaxY(topLeft, bottomRight);

            // convert the heightmap index bounds into world-space coordinates
            var minX = topLeft.X * Info.CellSpacing - Width / 2;
            var maxX = bottomRight.X * Info.CellSpacing - Width / 2;
            var minZ = -topLeft.Y * Info.CellSpacing + Depth / 2;
            var maxZ = -bottomRight.Y * Info.CellSpacing + Depth / 2;

            // adjust the bounds to get a very slight overlap of the bounding boxes
            minX -= tolerance;
            maxX += tolerance;
            minZ += tolerance;
            maxZ -= tolerance;

            // construct the new node and assign the world-space bounds of the terrain region
            var quadNode = new QuadTreeNode { Bounds = new BoundingBox(new Vector3(minX, minMaxY.X, minZ), new Vector3(maxX, minMaxY.Y, maxZ)) };

            var width = (int)Math.Floor((bottomRight.X - topLeft.X) / 2);
            var depth = (int)Math.Floor((bottomRight.Y - topLeft.Y) / 2);

            // we will recurse until the terrain regions match our logical terrain tile sizes
            if (width >= TileSize && depth >= TileSize) {
                quadNode.Children = new[] { BuildQuadTree(topLeft, new Vector2(topLeft.X + width, topLeft.Y + depth)), BuildQuadTree(new Vector2(topLeft.X + width, topLeft.Y), new Vector2(bottomRight.X, topLeft.Y + depth)), BuildQuadTree(new Vector2(topLeft.X, topLeft.Y + depth), new Vector2(topLeft.X + depth, bottomRight.Y)), BuildQuadTree(new Vector2(topLeft.X + width, topLeft.Y + depth), bottomRight) };
            } else {
                var center = (topLeft / 2 + bottomRight / 2) / 2;
                var mapX = (int)Math.Floor(center.X);
                var mapY = (int)Math.Floor(center.Y);
                quadNode.MapTile = GetTile(mapX, mapY);
            }

            return quadNode;
        }

        private Vector2 GetMinMaxY(Vector2 tl, Vector2 br) {
            var max = float.MinValue;
            var min = float.MaxValue;
            for (var x = (int)tl.X; x < br.X; x++) {
                for (var y = (int)tl.Y; y < br.Y; y++) {
                    min = Math.Min(min, _heightMap[y, x]);
                    max = Math.Max(max, _heightMap[y, x]);
                }
            }
            return new Vector2(min, max);
        }

        private void GenerateRandomTerrain() {
            var hm2 = new HeightMap(Info.HeightMapWidth, Info.HeightMapHeight, 2.0f);
            _heightMap.CreateRandomHeightMapParallel(Info.Seed, Info.NoiseSize1, Info.Persistence1, Info.Octaves1, true);
            hm2.CreateRandomHeightMapParallel(Info.Seed, Info.NoiseSize2, Info.Persistence2, Info.Octaves2, true);
            hm2.Cap(hm2.MaxHeight * 0.4f);
            _heightMap *= hm2;
        }

        public bool Intersect(Ray ray, ref Vector3 spherePos) {
            Vector3 ret;
            if (!_quadTree.Intersects(ray, out ret)) {
                return false;
            }
            ret.Y = Height(ret.X, ret.Z);
            spherePos = ret;
            return true;
        }
        public bool Intersect(Ray ray, ref Vector3 spherePos, ref MapTile mapPos) {
            Vector3 ret;
            QuadTreeNode ret2;
            if (!_quadTree.Intersects(ray, out ret, out ret2)) {
                return false;
            }
            ret.Y = Height(ret.X, ret.Z);
            spherePos = ret;
            mapPos = ret2.MapTile;
            return true;
        }
        public bool Intersect(Ray ray, ref MapTile mapPos) {
            QuadTreeNode ret;
            if (!_quadTree.Intersects(ray, out ret)) {
                return false;
            }
            mapPos = ret.MapTile;
            return true;
        }

    }
}
