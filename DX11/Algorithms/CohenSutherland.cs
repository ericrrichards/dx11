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

        public static Tuple<PointF, PointF> ClipSegment(RectangleF r, PointF p1, PointF p2) {
            // classify the endpoints of the line
            var outCodeP1 = ComputeOutCode(p1, r);
            var outCodeP2 = ComputeOutCode(p2, r);
            var accept = false;

            while (true) { // should only iterate twice, at most
                // Case 1:
                // both endpoints are within the clipping region
                if ((outCodeP1 | outCodeP2) == OutCode.Inside) {
                    accept = true;
                    break;
                }

                // Case 2:
                // both endpoints share an excluded region, impossible for a line between them to be within the clipping region
                if ((outCodeP1 & outCodeP2) != 0) {
                    break;
                }

                // Case 3:
                // The endpoints are in different regions, and the segment is partially within the clipping rectangle

                // Select one of the endpoints outside the clipping rectangle
                var outCode = outCodeP1 != OutCode.Inside ? outCodeP1 : outCodeP2;

                // calculate the intersection of the line with the clipping rectangle using parametric line equations
                var p = CalculateIntersection(r, p1, p2, outCode);

                // update the point after clipping and recaluculate outcode
                if (outCode == outCodeP1) {
                    p1 = p;
                    outCodeP1 = ComputeOutCode(p1, r);
                } else {
                    p2 = p;
                    outCodeP2 = ComputeOutCode(p2, r);
                }
            }
            // if clipping area contained a portion of the line
            if (accept) {
                return new Tuple<PointF, PointF>(p1, p2);
            }

            // the line did not intersect the clipping area
            return null;
        }

        private static PointF CalculateIntersection(RectangleF r, PointF p1, PointF p2, OutCode clipTo) {
            var dx = (p2.X - p1.X);
            var dy = (p2.Y - p1.Y);

            var slopeY = dx / dy; // slope to use for possibly-vertical lines
            var slopeX = dy / dx; // slope to use for possibly-horizontal lines

            if (clipTo.HasFlag(OutCode.Top)) {
                return new PointF(
                    p1.X + slopeY * (r.Top - p1.Y),
                    r.Top
                    );
            }
            if (clipTo.HasFlag(OutCode.Bottom)) {
                return new PointF(
                    p1.X + slopeY * (r.Bottom - p1.Y),
                    r.Bottom
                    );
            }
            if (clipTo.HasFlag(OutCode.Right)) {
                return new PointF(
                    r.Right,
                    p1.Y + slopeX * (r.Right - p1.X)
                    );
            }
            if (clipTo.HasFlag(OutCode.Left)) {
                return new PointF(
                    r.Left,
                    p1.Y + slopeX * (r.Left - p1.X)
                    );
            }
            throw new ArgumentOutOfRangeException("clipTo = " + clipTo);
        }
    }
}
