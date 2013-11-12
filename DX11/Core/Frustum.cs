using System.Linq;
using SlimDX;

namespace Core {
    public class Frustum {
        private readonly Plane[] _frustum;

        public const int Left = 0;
        public const int Right = 1;
        public const int Bottom = 2;
        public const int Top = 3;
        public const int Near = 4;
        public const int Far = 5;

        public Frustum(Matrix vp) {
            _frustum = new[] {
                //left
                new Plane(vp.M14 + vp.M11, vp.M24 + vp.M21, vp.M34 + vp.M31, vp.M44 + vp.M41),
                // right
                new Plane(vp.M14 - vp.M11, vp.M24 - vp.M21, vp.M34 - vp.M31, vp.M44 - vp.M41),
                // bottom
                new Plane(vp.M14 + vp.M12, vp.M24 + vp.M22, vp.M34 + vp.M32, vp.M44 + vp.M42),
                // top
                new Plane(vp.M14 - vp.M12, vp.M24 - vp.M22, vp.M34 - vp.M32, vp.M44 - vp.M42),
                //near
                new Plane(vp.M13, vp.M23, vp.M33, vp.M43),
                //far
                new Plane(vp.M14 - vp.M13, vp.M24 - vp.M23, vp.M34 - vp.M33, vp.M44 - vp.M43)
            };
            foreach (var plane in Planes) {
                plane.Normalize();
            }
        }

        

        public Plane[] Planes { get { return _frustum; } }

        public static Frustum FromViewProj(Matrix vp) {
            var ret = new Frustum(vp);
            return ret;
        }

        // Return values: 0 = no intersection, 
        //                1 = intersection, 
        //                2 = box is completely inside frustum
        public int Intersect(BoundingBox box) {
            var totalIn = 0;

            foreach (var plane in Planes) {
                var intersection = Plane.Intersects(plane, box);
                if (intersection == PlaneIntersectionType.Back) return 0;
                if (intersection == PlaneIntersectionType.Front) {
                    totalIn++;
                }
            }
            if (totalIn == 6) {
                return 2;
            }
            return 1;
        }
    }
}