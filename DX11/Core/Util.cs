using System;
using System.Runtime.InteropServices;
using SlimDX.D3DCompiler;
using SlimDX.DXGI;
using SlimDX.Direct3D11;
using Device = SlimDX.Direct3D11.Device;

namespace Core {
    using System.Runtime.InteropServices;

    using SlimDX;

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

        public static void ReleaseCom(ref ComObject x) {
            if (x != null) {
                x.Dispose();
                x = null;
            }
        }

        public static void ReleaseCom(ref SlimDX.Direct3D11.Buffer x) {
            if (x != null) {
                x.Dispose();
                x = null;
            }
        }

        public static void ReleaseCom(ref InputLayout x) {
            if (x != null) {
                x.Dispose();
                x = null;
            }
        }

        public static void ReleaseCom(ref ShaderBytecode x) {
            if (x != null) {
                x.Dispose();
                x = null;
            }
        }

        public static void ReleaseCom(ref SlimDX.Direct3D11.Effect x) {
            if (x != null) {
                x.Dispose();
                x = null;
            }
        }

        public static void ReleaseCom(ref RenderTargetView x) {
            if (x != null) {
                x.Dispose();
                x = null;
            }
        }

        public static void ReleaseCom(ref DepthStencilView x) {
            if (x != null) {
                x.Dispose();
                x = null;
            }
        }

        public static void ReleaseCom(ref SwapChain x) {
            if (x != null) {
                x.Dispose();
                x = null;
            }
        }

        public static void ReleaseCom(ref Texture2D x) {
            if (x != null) {
                x.Dispose();
                x = null;
            }
        }

        public static void ReleaseCom(ref DeviceContext x) {
            if (x != null) {
                x.Dispose();
                x = null;
            }
        }

        public static void ReleaseCom(ref Device x) {
            if (x != null) {
                x.Dispose();
                x = null;
            }
        }

        public static void ReleaseCom(ref RasterizerState x) {
            if (x != null) {
                x.Dispose();
                x = null;
            }
        }

        public static void ReleaseCom(ref ShaderResourceView x) {
            if (x != null) {
                x.Dispose();
                x = null;
            }
        }
    }
}