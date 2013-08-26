using System.Linq;
using SlimDX;

namespace Core {
    public class Frustum {
        private Plane[] _frustum = new Plane[6];
        public static Frustum FromViewProj(Matrix vp) {
            var ret = new Frustum {
                _frustum = new[] {
                    new Plane(vp.M14 + vp.M11, vp.M24 + vp.M21, vp.M34 + vp.M31, vp.M44 + vp.M41),
                    new Plane(vp.M14 - vp.M11, vp.M24 - vp.M21, vp.M34 - vp.M31, vp.M44 - vp.M41),
                    new Plane(vp.M14 - vp.M12, vp.M24 - vp.M22, vp.M34 - vp.M32, vp.M44 - vp.M42),
                    new Plane(vp.M14 + vp.M12, vp.M24 + vp.M22, vp.M34 + vp.M32, vp.M44 + vp.M42),
                    new Plane(vp.M13, vp.M23, vp.M33, vp.M43),
                    new Plane(vp.M14 - vp.M13, vp.M24 - vp.M23, vp.M34 - vp.M33, vp.M44 - vp.M43)
                }
            };
            foreach (var plane in ret._frustum) {
                plane.Normalize();
            }


            return ret;
        }
        
        // Return values: 0 = no intersection, 
        //                1 = intersection, 
        //                2 = box is completely inside frustum
        public int Intersect(BoundingBox box) {
            var totalIn = 0;
            
            foreach (var plane in _frustum) {
                Vector3 c1 = new Vector3(), c2 = new Vector3();
                if (plane.Normal.X > 0.0f) {
                    c1.X = box.Maximum.X;
                    c2.X = box.Minimum.X;
                } else {
                    c1.X = box.Minimum.X;
                    c2.X = box.Maximum.X;
                }
                if (plane.Normal.Y > 0.0f) {
                    c1.Y = box.Maximum.Y;
                    c2.Y = box.Minimum.Y;
                } else {
                    c1.Y = box.Minimum.Y;
                    c2.Y = box.Maximum.Y;
                }
                if (plane.Normal.Z > 0.0f) {
                    c1.Z = box.Maximum.Z;
                    c2.Z = box.Minimum.Z;
                } else {
                    c1.Z = box.Minimum.Z;
                    c2.Z = box.Maximum.Z;
                }
                var d1 = Plane.DotCoordinate(plane, c1);
                var d2 = Plane.DotCoordinate(plane, c2);
                if (d1 < 0.0f && d2 < 0.0f) {
                    return 0;
                }
                if (d1 >= 0.0f && d2 >= 0.0f) {
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