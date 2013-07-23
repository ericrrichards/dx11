using System;
using System.Runtime.InteropServices;

namespace Core {
    using System.Runtime.InteropServices;

    using SlimDX;

    public static class Util {
        public static void ReleaseCom(ComObject x) {
            if (x != null) {
                x.Dispose();
                x = null;
            }
        }
        public static int LowWord(this int i) {
            return i & 0xFFFF;
        }
        public static int HighWord(this int i) {
            return (i >> 16) & 0xFFFF;
        }

        public static Array GetArray(object o) {
            var len = Marshal.SizeOf(o);
            var arr = new byte[len];
            var ptr = Marshal.AllocHGlobal(len);
            Marshal.StructureToPtr(o, ptr, true);
            Marshal.Copy(ptr, arr, 0, len);
            Marshal.FreeHGlobal(ptr);
            return arr;

        }
    }
}