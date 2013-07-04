namespace Core {
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

    }
}