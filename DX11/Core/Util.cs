using System;
using System.Collections.Generic;
using System.Drawing;
using Assimp;
using SlimDX;
using Quaternion = SlimDX.Quaternion;

namespace Core {
    using System.Linq;
    using System.Runtime.InteropServices;

    using SlimDX.Direct3D11;
    using SlimDX.DXGI;

    using Device = SlimDX.Direct3D11.Device;
    using MapFlags = SlimDX.Direct3D11.MapFlags;
    using Resource = SlimDX.Direct3D11.Resource;

    public static class Util {
        private static byte[] _unmanagedStaging = new byte[1024];

        public static byte[] GetArray(object o) {
            Array.Clear(_unmanagedStaging, 0, _unmanagedStaging.Length);
            var len = Marshal.SizeOf(o);
            if (len >= _unmanagedStaging.Length) {
                _unmanagedStaging = new byte[len];
            }
            var ptr = Marshal.AllocHGlobal(len);
            Marshal.StructureToPtr(o, ptr, true);
            Marshal.Copy(ptr, _unmanagedStaging, 0, len);
            Marshal.FreeHGlobal(ptr);
            return _unmanagedStaging;

        }

        public static Vector3 Center(this BoundingBox b) {
            return (b.Maximum + b.Minimum) / 2;
        }

        public static Vector3 ToVector3(this Vector4 v) {
            return new Vector3(v.X, v.Y, v.Z);
        }
        public static Vector3 ToVector3(this Vector3D v) {
            return new Vector3(v.X, v.Y, v.Z);
        }
        public static Vector2 ToVector2(this Vector3D v) {
            return new Vector2(v.X, v.Y);
        }
        public static Material ToMaterial(this Assimp.Material m) {
            var ret = new Material {
                Ambient = new Color4(m.ColorAmbient.A, m.ColorAmbient.R, m.ColorAmbient.G, m.ColorAmbient.B),
                Diffuse = new Color4(m.ColorDiffuse.A, m.ColorAmbient.R, m.ColorAmbient.G, m.ColorAmbient.B),
                Specular = new Color4(m.Shininess, m.ColorSpecular.R, m.ColorSpecular.G, m.ColorSpecular.B),
                Reflect = new Color4(m.ColorReflective.A, m.ColorReflective.R, m.ColorReflective.G, m.ColorReflective.B)
            };

            if (ret.Ambient == new Color4(0, 0, 0, 0)) {
                ret.Ambient = Color.Gray;
            }
            if (ret.Diffuse == new Color4(0, 0, 0, 0) || ret.Diffuse == Color.Black) {
                ret.Diffuse = Color.White;
            }

            if (m.ColorSpecular == new Color4D(0, 0, 0, 0) || m.ColorSpecular == new Color4D(0, 0, 0)) {
                ret.Specular = new Color4(ret.Specular.Alpha, 0.5f, 0.5f, 0.5f);
            }

            return ret;
        }
        public static Matrix ToMatrix(this Matrix4x4 m) {
            var ret = Matrix.Identity;
            for (int i = 0; i < 4; i++) {
                for (int j = 0; j < 4; j++) {
                    ret[i, j] = m[i + 1, j + 1];
                }
            }
            return ret;
        }
        public static Quaternion ToQuat(this Assimp.Quaternion q) {
            return new Quaternion(q.X, q.Y, q.Z, q.W);
        }
        public static int LowWord(this int i) {
            return i & 0xFFFF;
        }

        public static int HighWord(this int i) {
            return (i >> 16) & 0xFFFF;
        }

        public static void ReleaseCom<T>(ref T x) where T : class, IDisposable {
            if (x != null) {
                x.Dispose();
                x = null;
            }
        }
        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(System.Windows.Forms.Keys vKey);

        public static bool IsKeyDown(System.Windows.Forms.Keys key) {
            return (GetAsyncKeyState(key) & 0x8000) != 0;
        }

        public static ShaderResourceView CreateTexture2DArraySRV(Device device, DeviceContext immediateContext, string[] filenames, Format format, FilterFlags filter = FilterFlags.None, FilterFlags mipFilter = FilterFlags.Linear) {
            var srcTex = new Texture2D[filenames.Length];
            for (int i = 0; i < filenames.Length; i++) {
                var loadInfo = new ImageLoadInformation {
                    FirstMipLevel = 0,
                    Usage = ResourceUsage.Staging,
                    BindFlags = BindFlags.None,
                    CpuAccessFlags = CpuAccessFlags.Write | CpuAccessFlags.Read,
                    OptionFlags = ResourceOptionFlags.None,
                    Format = format,
                    FilterFlags = filter,
                    MipFilterFlags = mipFilter,
                };
                srcTex[i] = Texture2D.FromFile(device, filenames[i], loadInfo);
            }
            var texElementDesc = srcTex[0].Description;

            var texArrayDesc = new Texture2DDescription {
                Width = texElementDesc.Width,
                Height = texElementDesc.Height,
                MipLevels = texElementDesc.MipLevels,
                ArraySize = srcTex.Length,
                Format = texElementDesc.Format,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };

            var texArray = new Texture2D(device, texArrayDesc);
            texArray.DebugName = "texture array: + " + filenames.Aggregate((i, j) => i + ", " + j);
            for (int texElement = 0; texElement < srcTex.Length; texElement++) {
                for (int mipLevel = 0; mipLevel < texElementDesc.MipLevels; mipLevel++) {
                    var mappedTex2D = immediateContext.MapSubresource(srcTex[texElement], mipLevel, 0, MapMode.Read, MapFlags.None);

                    immediateContext.UpdateSubresource(
                        mappedTex2D,
                        texArray,
                        Resource.CalculateSubresourceIndex(mipLevel, texElement, texElementDesc.MipLevels)
                        );
                    immediateContext.UnmapSubresource(srcTex[texElement], mipLevel);
                }
            }
            var viewDesc = new ShaderResourceViewDescription {
                Format = texArrayDesc.Format,
                Dimension = ShaderResourceViewDimension.Texture2DArray,
                MostDetailedMip = 0,
                MipLevels = texArrayDesc.MipLevels,
                FirstArraySlice = 0,
                ArraySize = srcTex.Length
            };

            var texArraySRV = new ShaderResourceView(device, texArray, viewDesc);

            ReleaseCom(ref texArray);
            for (int i = 0; i < srcTex.Length; i++) {
                ReleaseCom(ref srcTex[i]);
            }

            return texArraySRV;
        }

        public static ShaderResourceView CreateRandomTexture1DSRV(Device device) {
            var randomValues = new List<Vector4>();
            for (int i = 0; i < 1024; i++) {
                randomValues.Add(new Vector4(MathF.Rand(-1.0f, 1.0f), MathF.Rand(-1.0f, 1.0f), MathF.Rand(-1.0f, 1.0f), MathF.Rand(-1.0f, 1.0f)));
            }
            var texDesc = new Texture1DDescription() {
                ArraySize = 1,
                BindFlags = BindFlags.ShaderResource,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = Format.R32G32B32A32_Float,
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                Usage = ResourceUsage.Immutable,
                Width = 1024
            };
            var randTex = new Texture1D(device, texDesc, new DataStream(randomValues.ToArray(), false, false));

            var viewDesc = new ShaderResourceViewDescription() {
                Format = texDesc.Format,
                Dimension = ShaderResourceViewDimension.Texture1D,
                MipLevels = texDesc.MipLevels,
                MostDetailedMip = 0
            };
            var randTexSRV = new ShaderResourceView(device, randTex, viewDesc);
            ReleaseCom(ref randTex);
            return randTexSRV;
        }
    }
}