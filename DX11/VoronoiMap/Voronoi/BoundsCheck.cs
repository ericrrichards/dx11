using System;
using SlimDX;

namespace VoronoiMap.Voronoi {
    [Flags]
    public enum BoundsCheck {
        None = 0,
        Top = 1,
        Bottom = 2,
        Left = 4,
        Right = 8,
    }
    public class BoundsChecker {
        public static BoundsCheck Check(Vector2 point, Rectangle bounds) {
            var value = BoundsCheck.None;
            if (Equals(point.X, bounds.Left)) {
                value |= BoundsCheck.Left;
            }
            if (Equals(point.X, bounds.Right)) {
                value |= BoundsCheck.Right;
            }
            if (Equals(point.Y, bounds.Top)) {
                value |= BoundsCheck.Top;
            }
            if (Equals(point.Y, bounds.Bottom)) {
                value |= BoundsCheck.Bottom;
            }

            return value;
        }
    }

}