using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algorithms {
    /// <summary>
    /// http://en.wikipedia.org/wiki/Cohen%E2%80%93Sutherland_algorithm
    /// </summary>
    public static class CohenSutherland {

        [Flags]
        enum OutCode {
            Inside = 0,
            Left = 1,
            Right = 2,
            Bottom = 4,
            Top = 8
        }

        private static OutCode ComputeOutCode(float x, float y, RectangleF r) {
            var code = OutCode.Inside;

            if (x < r.Left) code |= OutCode.Left;
            if (x > r.Right) code |= OutCode.Right;
            if (y < r.Top) code |= OutCode.Top;
            if (y > r.Bottom) code |= OutCode.Bottom;

            return code;
        }
        private static OutCode ComputeOutCode(PointF p, RectangleF r) { return ComputeOutCode(p.X, p.Y, r); }

        public static bool ClipSegment(RectangleF r, ref PointF p1, ref PointF p2 ) {
            var outCodeP1 = ComputeOutCode(p1, r);
            var outCodeP2 = ComputeOutCode(p2, r);
            var accept = false;

            var x0 = p1.X;
            var x1 = p2.X;
            var y0 = p1.Y;
            var y1 = p2.Y;
            var xmin = r.Left;
            var xmax = r.Right;
            var ymin = r.Top;
            var ymax = r.Bottom;

            while (true) {
                if ((outCodeP1 | outCodeP2) == OutCode.Inside) {
                    accept = true;
                    break;
                }
                if ((outCodeP1 & outCodeP2) != 0) {
                    break;
                }



                var outCode = outCodeP1 != OutCode.Inside ? outCodeP1 : outCodeP2;

                float x = 0, y = 0;

                if (outCode.HasFlag(OutCode.Top)) {
                    x = x0 + (x1 - x0) * (ymin - y0) / (y1 - y0);
                    y = ymin;
                } else if (outCode.HasFlag(OutCode.Bottom)) {
                    x = x0 + (x1 - x0) * (ymax - y0) / (y1 - y0);
                    y = ymax;
                } else if (outCode.HasFlag(OutCode.Right)) {
                    y = y0 + (y1 - y0) * (xmax - x0) / (x1 - x0);
                    x = xmax;
                } else if (outCode.HasFlag(OutCode.Left)) {
                    y = y0 + (y1 - y0) * (xmin - x0) / (x1 - x0);
                    x = xmin;
                }

                if (outCode == outCodeP1) {
                    x0 = x;
                    y0 = y;
                    outCodeP1 = ComputeOutCode(x0, y0, r);
                } else {
                    x1 = x;
                    y1 = y;
                    outCodeP2 = ComputeOutCode(x1, y1, r);
                }

            }
            if (!accept) return false;

            p1.X = x0;
            p1.Y = y0;
            p2.X = x1;
            p2.Y = y1;
            return true;
        }


    }
}
