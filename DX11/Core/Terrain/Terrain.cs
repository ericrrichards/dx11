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

    public class Terrain : DisposableClass {
        public const int CellsPerPatch = 64;


        private bool _disposed;


        public float Width { get { return (Info.HeightMapWidth - 1) * Info.CellSpacing; } }
        public float Depth { get { return (Info.HeightMapHeight - 1) * Info.CellSpacing; } }

        public InitInfo Info { get; private set; }
        public HeightMap HeightMap { get; private set; }
        public Image HeightMapImg { get { return HeightMap.Bitmap; } }
        public QuadTree QuadTree { get; private set; }

        private TerrainRenderer _renderer;
        public TerrainRenderer Renderer { get { return _renderer; } }
        public MapTile[] Tiles { get { return _tiles; } }
        public int WidthInTiles { get { return _widthInTiles; } }
        public int HeightInTiles { get { return _heightInTiles; } }
        private static Heuristics.Distance h;

        private MapTile[] _tiles;
        private int _widthInTiles;
        private int _heightInTiles;
        private const int TileSize = 2;

        public Terrain() {
            h = Heuristics.DiagonalDistance2;
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

        #region Utility Functions

        public float Height(float x, float z) {
            var c = (x + 0.5f * Width) / Info.CellSpacing;
            var d = (z - 0.5f * Depth) / -Info.CellSpacing;
            var row = (int)Math.Floor(d);
            var col = (int)Math.Floor(c);

            var h00 = HeightMap[row, col];
            var h01 = HeightMap[row, col + 1];
            var h10 = HeightMap[(row + 1), col];
            var h11 = HeightMap[(row + 1), col + 1];

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


        private bool Within(Point p) {
            return p.X >= 0 && p.X < _widthInTiles && p.Y >= 0 && p.Y < _heightInTiles;
        }

        public MapTile GetTile(Point point) { return GetTile(point.X, point.Y); }

        public MapTile GetTile(int x, int y) {
            return _tiles == null ? null : _tiles[x + y * _heightInTiles];
        }

        private Vector2 GetMinMaxY(Vector2 tl, Vector2 br) {
            var max = float.MinValue;
            var min = float.MaxValue;
            for (var x = (int)tl.X; x < br.X; x++) {
                for (var y = (int)tl.Y; y < br.Y; y++) {
                    min = Math.Min(min, HeightMap[y, x]);
                    max = Math.Max(max, HeightMap[y, x]);
                }
            }
            return new Vector2(min, max);
        }
        #endregion

        public void Init(Device device, DeviceContext dc, InitInfo info) {
            D3DApp.GD3DApp.ProgressUpdate.Draw(0, "Initializing terrain");


            Info = info;


            HeightMap = new HeightMap(Info.HeightMapWidth, Info.HeightMapHeight, Info.HeightScale);
            if (!string.IsNullOrEmpty(Info.HeightMapFilename)) {
                D3DApp.GD3DApp.ProgressUpdate.Draw(0.1f, "Loading terrain from file");
                HeightMap.LoadHeightmap(Info.HeightMapFilename);
            } else {
                D3DApp.GD3DApp.ProgressUpdate.Draw(0.1f, "Generating random terrain");
                GenerateRandomTerrain();
                D3DApp.GD3DApp.ProgressUpdate.Draw(0.50f, "Smoothing terrain");
                HeightMap.Smooth(true);
            }
            InitTileMap();
            D3DApp.GD3DApp.ProgressUpdate.Draw(0.55f, "Building picking quadtree...");
            QuadTree = new QuadTree {
                Root = BuildQuadTree(new Vector2(0, 0), new Vector2((Info.HeightMapWidth - 1), (Info.HeightMapHeight - 1)))
            };


            Renderer.Init(device, dc, this);

        }

        private void GenerateRandomTerrain() {
            var hm2 = new HeightMap(Info.HeightMapWidth, Info.HeightMapHeight, 2.0f);
            HeightMap.CreateRandomHeightMapParallel(Info.Seed, Info.NoiseSize1, Info.Persistence1, Info.Octaves1, true);
            hm2.CreateRandomHeightMapParallel(Info.Seed, Info.NoiseSize2, Info.Persistence2, Info.Octaves2, true);
            hm2.Cap(hm2.MaxHeight * 0.4f);
            HeightMap *= hm2;
        }

        #region Pathfinding

        private void InitTileMap() {
            ResetTileMap();
            SetTilePositionsAndTypes();
            CalculateWalkability();
            ConnectNeighboringTiles();
            CreateTileSets();

        }

        private void ResetTileMap() {
            _widthInTiles = Info.HeightMapWidth / TileSize;
            _heightInTiles = Info.HeightMapHeight / TileSize;
            _tiles = new MapTile[_widthInTiles * _heightInTiles];
            for (var i = 0; i < _tiles.Length; i++) {
                _tiles[i] = new MapTile();
            }
        }

        private void SetTilePositionsAndTypes() {
            for (var y = 0; y < _heightInTiles; y++) {
                for (var x = 0; x < _widthInTiles; x++) {
                    var tile = GetTile(x, y);
                    tile.MapPosition = new Point(x, y);
                    // Calculate world position of tile center
                    var worldX = (x * Info.CellSpacing * TileSize) + (Info.CellSpacing * TileSize / 2) - (Width / 2);
                    var worldZ = (-y * Info.CellSpacing * TileSize) - (Info.CellSpacing * TileSize / 2) + (Depth / 2);
                    var height = Height(worldX, worldZ);
                    tile.WorldPos = new Vector3(worldX, height, worldZ);

                    // Set tile type
                    if (tile.Height < HeightMap.MaxHeight * (0.05f)) {
                        tile.Type = 0;
                    } else if (tile.Height < HeightMap.MaxHeight * (0.4f)) {
                        tile.Type = 1;
                    } else if (tile.Height < HeightMap.MaxHeight * (0.75f)) {
                        tile.Type = 2;
                    } else {
                        tile.Type = 3;
                    }
                }
            }
        }

        private void CalculateWalkability() {
            for (var y = 0; y < _heightInTiles; y++) {
                for (var x = 0; x < _widthInTiles; x++) {
                    var tile = GetTile(x, y);

                    if (tile == null) {
                        continue;
                    }
                    var p = new[] {
                new Point(x - 1, y - 1), new Point(x, y - 1), new Point(x + 1, y - 1),
                new Point(x - 1, y), new Point(x + 1, y),
                new Point(x - 1, y + 1), new Point(x, y + 1), new Point(x + 1, y + 1)
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
                        // ignore neighbors on the same plane as this tile
                        if (v <= 0.01f) {
                            continue;
                        }
                        variance += v * v;


                        nr++;

                    }
                    // prevent divide by 0
                    if (nr == 0) nr = 1;
                    variance /= nr;

                    tile.Walkable = variance < MapTile.MaxSlope;
                }
            }
        }

        private void ConnectNeighboringTiles() {
            for (var y = 0; y < _heightInTiles; y++) {
                for (var x = 0; x < _widthInTiles; x++) {
                    var tile = GetTile(x, y);
                    if (tile == null || !tile.Walkable) {
                        continue;
                    }
                    for (var i = 0; i < 8; i++) {
                        tile.Edges[i] = null;
                    }
                    var p = new[] {
                        new Point(x - 1, y - 1), new Point(x, y - 1), new Point(x + 1, y - 1),
                        new Point(x - 1, y), new Point(x + 1, y),
                        new Point(x - 1, y + 1), new Point(x, y + 1), new Point(x + 1, y + 1)
                    };
                    for (var i = 0; i < 8; i++) {
                        var point = p[i];
                        if (!Within(point)) {
                            continue;
                        }
                        var neighbor = GetTile(point);
                        if (neighbor != null && neighbor.Walkable) {
                            tile.Edges[i] = MapEdge.Create(tile, neighbor);
                        }
                    }
                }
            }
        }

        private void CreateTileSets() {
            var setNo = 0;
            var unvisited = new HashSet<MapTile>();
            // scan the tiles, to create the list of walkable tiles to consider
            // assign unwalkable or unconnected tiles to unique negative tilesets
            for (var y = 0; y < _heightInTiles; y++) {
                for (var x = 0; x < _widthInTiles; x++) {
                    var tile = GetTile(x, y);
                    if (tile.Edges.Any(e => e != null)) {
                        if (tile.Walkable) {
                            unvisited.Add(tile);
                        } else {
                            tile.Set = --setNo;
                        }
                    } else {
                        tile.Set = --setNo;
                    }
                }
            }
            setNo = 0;
            // stack for depth-first search
            var stack = new Stack<MapTile>();

            while (unvisited.Any()) {
                // extract the first unvisited node in order to seed the depth-first search
                var newFirst = unvisited.First();
                stack.Push(newFirst);
                unvisited.Remove(newFirst);

                while (stack.Any()) {
                    // perform the depth-first search
                    var next = stack.Pop();
                    next.Set = setNo;
                    // Get the neighbors of this node, where the neighbor is connected to this node, 
                    // and has not been visited yet
                    var neighbors = next.Edges.Where(e => e != null && unvisited.Contains(e.Node2)).Select(e => e.Node2);
                    foreach (var mapTile in neighbors) {
                        stack.Push(mapTile);
                        unvisited.Remove(mapTile);
                    }
                }
                setNo++;
            }
        }

        public List<MapTile> GetPath(Point start, Point goal) {
            var startTile = GetTile(start);
            var goalTile = GetTile(goal);

            // check that the start and goal positions are valid, and are not the same
            if (!Within(start) || !Within(goal) || start == goal || startTile == null || goalTile == null) {
                return new List<MapTile>();
            }
            // Check that start and goal are walkable and that a path can exist between them
            if (startTile.Set != goalTile.Set) {
                return new List<MapTile>();
            }


            // reset costs
            foreach (var t in _tiles) {
                t.F = t.G = float.MaxValue;
            }
            var open = new PriorityQueue<MapTile>(_tiles.Length);
            var closed = new HashSet<MapTile>();

            startTile.G = 0;
            startTile.F = h(start, goal);

            open.Enqueue(startTile, startTile.F);

            MapTile current = null;
            while (open.Any() && current != goalTile) {
                current = open.Dequeue();
                closed.Add(current);
                for (var i = 0; i < 8; i++) {
                    var edge = current.Edges[i];

                    if (edge == null) {
                        continue;
                    }
                    var neighbor = edge.Node2;
                    var cost = current.G + edge.Cost;



                    if (open.Contains(neighbor) && cost < neighbor.G) {
                        open.Remove(neighbor);
                    }
                    if (closed.Contains(neighbor) && cost < neighbor.G) {
                        closed.Remove(neighbor);
                    }
                    if (!open.Contains(neighbor) && !closed.Contains(neighbor)) {
                        neighbor.G = cost;
                        var f = cost + h(neighbor.MapPosition, goal);
                        open.Enqueue(neighbor, f);
                        neighbor.Parent = current;

                    }
                }
            }
            System.Diagnostics.Debug.Assert(current == goalTile);
            var path = new List<MapTile>();


            while (current != startTile) {
                path.Add(current);
                current = current.Parent;
            }
            path.Reverse();
            return path;
        }
        #endregion

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
                quadNode.Children = new[] {
                    BuildQuadTree(topLeft, new Vector2(topLeft.X + width, topLeft.Y + depth)), 
                    BuildQuadTree(new Vector2(topLeft.X + width, topLeft.Y), new Vector2(bottomRight.X, topLeft.Y + depth)), 
                    BuildQuadTree(new Vector2(topLeft.X, topLeft.Y + depth), new Vector2(topLeft.X + depth, bottomRight.Y)), 
                    BuildQuadTree(new Vector2(topLeft.X + width, topLeft.Y + depth), bottomRight)
                };
            } else {
                // set the maptile corresponding to this leaf node of the quad tree
                var center = topLeft / TileSize;

                var mapX = (int)Math.Floor(center.X);
                var mapY = (int)Math.Floor(center.Y);
                quadNode.MapTile = GetTile(mapX, mapY);


            }

            return quadNode;
        }



        #region Intersection tests

        public bool Intersect(Ray ray, ref Vector3 worldPos, ref MapTile mapPos) {
            Vector3 ret;
            QuadTreeNode ret2;
            if (!QuadTree.Intersects(ray, out ret, out ret2)) {
                return false;
            }
            ret.Y = Height(ret.X, ret.Z);
            worldPos = ret;
            mapPos = ret2.MapTile;
            return true;
        }

        #endregion

    }
}
