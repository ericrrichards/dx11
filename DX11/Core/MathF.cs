using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core {
    using SlimDX;

    public static class MathF {
        private static Random _rand = new Random();
        public const float PI = (float) Math.PI;

        public static float Sin(float a) {
            return (float) Math.Sin(a);
        }
        public static float Cos(float a) {
            return (float) Math.Cos(a);
        }
        public static float ToRadians(float degrees) {
            return PI * degrees / 180.0f;
        }
        public static float ToDegrees(float radians) {
            return radians * (180.0f / PI);
        }

        public static float Clamp(float value, float min, float max) {
            return Math.Max(min, Math.Min(value, max));
        }

        public static int Rand() {
            return _rand.Next();
        }

        public static float Rand(float min, float max) {
            return min + (float)_rand.NextDouble() * (max - min);
        }

        public static Matrix InverseTranspose(Matrix m) {
            return Matrix.Transpose(Matrix.Invert(m));
        }

        public static float Tan(float a) {
            return (float)Math.Tan(a);
        }

        public static float Atan(float f) {
            return (float)Math.Atan(f);
        }


        // heightmap functions
        public static float Noise(int x) {
            x = (x << 13) ^ x;
            return (1.0f - ((x * (x * x * 15731) + 1376312589) & 0x7fffffff) / 1073741824.0f);
        }

        public static float CosInterpolate(float v1, float v2, float a) {
            var angle = a * PI;
            var prc = (1.0f - Cos(angle)) * 0.5f;
            return v1 * (1.0f - prc) + v2 * prc;
        }
    }
}
