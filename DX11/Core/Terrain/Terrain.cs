namespace Core.Terrain {
    #region

    using System;
    using System.Drawing;

    using SlimDX;
    using SlimDX.Direct3D11;

    using Device = SlimDX.Direct3D11.Device;

    #endregion

    public class Terrain : DisposableClass {
        public const int CellsPerPatch = 64;
        private bool _disposed;
        private HeightMap _heightMap;

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

            D3DApp.GD3DApp.ProgressUpdate.Draw(0.55f, "Building picking quadtree...");
            _quadTree = new QuadTree { Root = BuildQuadTree(new Vector2(0, 0), new Vector2((Info.HeightMapWidth - 1), (Info.HeightMapHeight - 1))) };




            Renderer.Init(device, dc, this);
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
            const int tileSize = 2;
            if (width >= tileSize && depth >= tileSize) {
                quadNode.Children = new[] { BuildQuadTree(topLeft, new Vector2(topLeft.X + width, topLeft.Y + depth)), BuildQuadTree(new Vector2(topLeft.X + width, topLeft.Y), new Vector2(bottomRight.X, topLeft.Y + depth)), BuildQuadTree(new Vector2(topLeft.X, topLeft.Y + depth), new Vector2(topLeft.X + depth, bottomRight.Y)), BuildQuadTree(new Vector2(topLeft.X + width, topLeft.Y + depth), bottomRight) };
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
    }
}
