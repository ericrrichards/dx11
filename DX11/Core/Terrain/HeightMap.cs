namespace Core.Terrain {
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;

    using SlimDX;
    using SlimDX.DXGI;
    using SlimDX.Direct3D11;

    using Device = SlimDX.Direct3D11.Device;

    public class HeightMap {
        private float[] _heightMap;
        private Bitmap _bitmap;
        public int HeightMapWidth { get; private set; }
        public int HeightMapHeight { get; private set; }
        public float MaxHeight { get; private set; }
        public Image Bitmap {
            get { return _bitmap; }
        }

        public HeightMap(int width, int height, float maxHeight) {
            HeightMapWidth = width;
            HeightMapHeight = height;
            MaxHeight = maxHeight;
            _heightMap = new float[HeightMapWidth * HeightMapHeight];
        }

        public float this[int row, int col] {
            get {
                if (InBounds(row, col)) {
                    return _heightMap[row * HeightMapHeight + col];
                }
                return 0.0f;
            }
            private set {
                if (InBounds(row, col)) {
                    _heightMap[row * HeightMapHeight + col] = value;
                }
            }
        }

        public void LoadHeightmap(string heightMapFilename) {
            var input = File.ReadAllBytes(heightMapFilename);

            _heightMap = input.Select(i => (i / 255.0f * MaxHeight)).ToArray();
        }

        public void Smooth(bool drawProgress = false) {
            var dest = new float[HeightMapHeight * HeightMapWidth];
            for (var i = 0; i < HeightMapHeight; i++) {
                for (var j = 0; j < HeightMapWidth; j++) {
                    dest[i * HeightMapHeight + j] = Average(i, j);
                }
                if (drawProgress) {
                    D3DApp.GD3DApp.ProgressUpdate.Draw(0.50f + 0.25f * ((float)i / HeightMapHeight), "Smoothing terrain");
                }
            }
            _heightMap = dest;
        }

        private float Average(int row, int col) {
            var avg = 0.0f;
            var num = 0.0f;
            for (var m = row - 1; m <= row + 1; m++) {
                for (var n = col - 1; n <= col + 1; n++) {
                    if (!InBounds(m, n)) continue;

                    avg += _heightMap[m * HeightMapHeight + n];
                    num++;
                }
            }
            return avg / num;
        }

        internal bool InBounds(int row, int col) {
            return row >= 0 && row < HeightMapHeight && col >= 0 && col < HeightMapWidth;
        }

        public ShaderResourceView BuildHeightmapSRV(Device device) {
            var texDec = new Texture2DDescription {
                ArraySize = 1,
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = Format.R16_Float,
                SampleDescription = new SampleDescription(1, 0),
                Height = HeightMapHeight,
                Width = HeightMapWidth,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                Usage = ResourceUsage.Default
            };
            var hmap = Half.ConvertToHalf(_heightMap.ToArray());

            var hmapTex = new Texture2D(
                device,
                texDec,
                new DataRectangle(
                    HeightMapWidth * Marshal.SizeOf(typeof(Half)),
                    new DataStream(hmap.ToArray(), false, false)
                    )
                );
            hmapTex.DebugName = "heightmap texture";
            BuildHeightMapThumb();

            var srvDesc = new ShaderResourceViewDescription {
                Format = texDec.Format,
                Dimension = ShaderResourceViewDimension.Texture2D,
                MostDetailedMip = 0,
                MipLevels = -1
            };

            var srv = new ShaderResourceView(device, hmapTex, srvDesc);


            Util.ReleaseCom(ref hmapTex);
            return srv;
        }

        private void BuildHeightMapThumb() {
            var userBuffer = _heightMap.Select(h => (byte)((h / MaxHeight) * 255)).ToArray();
            _bitmap = new Bitmap(HeightMapWidth - 1, HeightMapHeight - 1, PixelFormat.Format24bppRgb);
            var data = _bitmap.LockBits(
                new Rectangle(0, 0, _bitmap.Width, _bitmap.Height), 
                ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            var ptr = data.Scan0;
            var bytes = data.Stride * _bitmap.Height;
            var values = new byte[bytes];

            Marshal.Copy(ptr, values, 0, bytes);

            for (var i = 0; i < values.Length; i++) {
                values[i] = userBuffer[i / 3];
            }
            Marshal.Copy(values, 0, ptr, bytes);

            _bitmap.UnlockBits(data);
        }

        // procedural heightmap stuff
        public static HeightMap operator *(HeightMap lhs, HeightMap rhs) {
            var hm = new HeightMap(lhs.HeightMapWidth, lhs.HeightMapHeight, lhs.MaxHeight);
            for (var y = 0; y < lhs.HeightMapHeight; y++) {
                for (var x = 0; x < lhs.HeightMapWidth; x++) {
                    var a = lhs[y, x] / lhs.MaxHeight;
                    var b = 1.0f;
                    if (rhs.InBounds(y, x)) {
                        b = rhs[y, x] / rhs.MaxHeight;
                    }
                    hm[y, x] = a * b * hm.MaxHeight;
                }
            }
            return hm;
        }

        public void Cap(float capHeight) {
            MaxHeight = 0.0f;
            for (var y = 0; y < HeightMapHeight; y++) {
                for (var x = 0; x < HeightMapWidth; x++) {
                    var index = x + y * HeightMapHeight;
                    _heightMap[index] -= capHeight;
                    if (_heightMap[index] < 0.0f) {
                        _heightMap[index] = 0.0f;
                    }
                    if (_heightMap[index] > MaxHeight) {
                        MaxHeight = _heightMap[index];
                    }
                }
            }
        }

        public void CreateRandomHeightMap(int seed, float noiseSize, float persistence, int octaves) {

            for (var y = 0; y < HeightMapHeight; y++) {
                for (var x = 0; x < HeightMapWidth; x++) {


                    var xf = (x / (float)HeightMapWidth) * noiseSize;
                    var yf = (y / (float)HeightMapHeight) * noiseSize;

                    var total = 0.0f;
                    for (var i = 0; i < octaves; i++) {
                        var freq = (float)Math.Pow(2.0f, i);
                        var amp = (float)Math.Pow(persistence, i);
                        var tx = xf * freq;
                        var ty = yf * freq;
                        var txi = (int)tx;
                        var tyi = (int)ty;
                        var fracX = tx - txi;
                        var fracY = ty - tyi;

                        var v1 = MathF.Noise(txi + tyi * 57 + seed);
                        var v2 = MathF.Noise(txi + 1 + tyi * 57 + seed);
                        var v3 = MathF.Noise(txi + (tyi + 1) * 57 + seed);
                        var v4 = MathF.Noise(txi + 1 + (tyi + 1) * 57 + seed);

                        var i1 = MathF.CosInterpolate(v1, v2, fracX);
                        var i2 = MathF.CosInterpolate(v3, v4, fracX);
                        total += MathF.CosInterpolate(i1, i2, fracY) * amp;
                    }
                    var b = (int)(128 + total * 128.0f);
                    if (b < 0) b = 0;
                    if (b > 255) b = 255;

                    _heightMap[x + y * HeightMapHeight] = (b / 255.0f) * MaxHeight;
                }
            }
        }

        public void CreateRandomHeightMapParallel(int seed, float noiseSize, float persistence, int octaves, bool drawProgress = false) {

            for (var y = 0; y < HeightMapHeight; y++) {
                var tasks = new List<Action>();
                var y1 = y;


                for (var x = 0; x < HeightMapWidth; x++) {
                    var x1 = x;
                    tasks.Add(() => {
                        var xf = (x1 / (float)HeightMapWidth) * noiseSize;
                        var yf = (y1 / (float)HeightMapHeight) * noiseSize;

                        var total = 0.0f;
                        for (var i = 0; i < octaves; i++) {
                            var f = MathF.PerlinNoise2D(seed, persistence, i, xf, yf);
                            total += f;
                        }
                        var b = (int)(128 + total * 128.0f);
                        if (b < 0) b = 0;
                        if (b > 255) b = 255;

                        _heightMap[x1 + y1 * HeightMapHeight] = (b / 255.0f) * MaxHeight;
                    });
                }
                if (drawProgress) {
                    D3DApp.GD3DApp.ProgressUpdate.Draw(0.1f + 0.40f * ((float)y / HeightMapHeight), "Generating random terrain");
                }
                Parallel.Invoke(tasks.ToArray());
            }
        }
    }
}