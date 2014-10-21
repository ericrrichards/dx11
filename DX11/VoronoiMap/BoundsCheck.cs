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
            if (Math.Abs(p.X - bounds.Left) < Geometry.Tolerance) {
                value |= Sides.Left;
            }
            if (Math.Abs(p.X - bounds.Right) < Geometry.Tolerance) {
                value |= Sides.Right;
            }
            if (Math.Abs(p.Y - bounds.Top) < Geometry.Tolerance) {
                value |= Sides.Top;
            }
            if (Math.Abs(p.Y - bounds.Bottom) < Geometry.Tolerance) {
                value |= Sides.Bottom;
            }
            return value;
        }
    }
}