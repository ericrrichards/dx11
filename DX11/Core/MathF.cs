using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core {
    public static class MathF {
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
    }
}
