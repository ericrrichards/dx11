using System;

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

        public static Matrix Shadow(Vector4 lightDir , Plane shadowPlane) {
            var dot = Vector3.Dot(shadowPlane.Normal, lightDir.ToVector3()) + shadowPlane.D* lightDir.W;
            var x = -lightDir.X;
            var y = -lightDir.Y;
            var z = -lightDir.Z;
            var w = -lightDir.W;
            var n = shadowPlane.Normal;
            var d = shadowPlane.D;

            var ret = new Matrix();
            ret.M11 = dot + x * n.X;
            ret.M12 = y * n.X;
            ret.M13 = z * n.X;
            ret.M14 = w * n.X;

            ret.M21 = x * n.Y;
            ret.M22 = dot + y * n.Y;
            ret.M23 = z * n.Y;
            ret.M24 = w * n.Y;

            ret.M31 = x * n.Z;
            ret.M32 = y * n.Z;
            ret.M33 = dot + z * n.Z;
            ret.M34 = w * n.Z;

            ret.M41 = x * d;
            ret.M42 = y * d;
            ret.M43 = z * d;
            ret.M44 = Vector3.Dot(shadowPlane.Normal, lightDir.ToVector3());

            return ret;
        }

        public static Vector3 ToVector3(this Vector4 v) {
            return new Vector3(v.X, v.Y, v.Z);
        }
    }
}