using System;

namespace Core {
    using System.Linq;
    using System.Runtime.InteropServices;

    using SlimDX.Direct3D11;
    using SlimDX.DXGI;

    using Device = SlimDX.Direct3D11.Device;
    using MapFlags = SlimDX.Direct3D11.MapFlags;
    using Resource = SlimDX.Direct3D11.Resource;

    public static class Util {
        public static byte[] GetArray(object o) {
            var len = Marshal.SizeOf(o);
            var arr = new byte[len];
            var ptr = Marshal.AllocHGlobal(len);
            Marshal.StructureToPtr(o, ptr, true);
            Marshal.Copy(ptr, arr, 0, len);
            Marshal.FreeHGlobal(ptr);
            return arr;

        }

        public static int LowWord(this int i) {
            return i & 0xFFFF;
        }

        public static int HighWord(this int i) {
            return (i >> 16) & 0xFFFF;
        }

        public static void ReleaseCom<T>(ref T x) where T: class, IDisposable{
            if (x != null) {
                x.Dispose();
                x = null;
            }
        }
        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vKey);

        public static bool IsKeyDown(System.Windows.Forms.Keys key) {
            return (GetAsyncKeyState((int)key) & 0x8000) != 0;
        }

        public static ShaderResourceView CreateTexture2DArraySRV(Device device, DeviceContext immediateContext, string[] filenames, Format format, FilterFlags filter=FilterFlags.None, FilterFlags mipFilter=FilterFlags.Linear) {
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
    }
}