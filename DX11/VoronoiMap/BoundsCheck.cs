using System;
using System.Drawing;

namespace VoronoiMap {
    public static class BoundsCheck {
        [Flags]
        public enum Sides {
            None = 0,
            Top = 1,
            Bottom = 2,
            Left = 4,
            Right = 8
        }

        public static Sides Check(Site p, RectangleF bounds) {
            var value = Sides.None;
            if (Math.Abs(p.X - bounds.Left) < float.Epsilon) {
                value |= Sides.Left;
            }
            if (Math.Abs(p.X - bounds.Right) < float.Epsilon) {
                value |= Sides.Right;
            }
            if (Math.Abs(p.Y - bounds.Top) < float.Epsilon) {
                value |= Sides.Top;
            }
            if (Math.Abs(p.Y - bounds.Bottom) < float.Epsilon) {
                value |= Sides.Bottom;
            }
            return value;
        }
    }
}