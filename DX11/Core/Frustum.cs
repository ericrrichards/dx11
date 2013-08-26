using System.Linq;
using SlimDX;

namespace Core {
    public class Frustum {
        public Vector3 Origin { get; set; }
        public Quaternion Orientation { get; set; }
        public float RightSlope { get; set; }
        public float LeftSlope { get; set; }
        public float TopSlope { get; set; }
        public float BottomSlope { get; set; }
        public float Near { get; set; }
        public float Far { get; set; }

        private static readonly Vector4[] HomogeneousPoints = {
            new Vector4(1, 0, 1, 1),
            new Vector4(-1, 0, 1, 1), 
            new Vector4(0, 1, 1, 1),
            new Vector4(0, -1, 1, 1),
            new Vector4(0, 0, 0, 1),
            new Vector4(0,0,1,1), 
        };
        public static Frustum FromProjection(Matrix proj) {
            var inverse = Matrix.Invert(proj);

            var points = Vector4.Transform(HomogeneousPoints, ref inverse);

            points[0] = points[0] * (1.0f / points[0].Z);
            points[1] = points[1] * (1.0f / points[1].Z);
            points[2] = points[2] * (1.0f / points[2].Z);
            points[3] = points[3] * (1.0f / points[3].Z);
            points[4] = points[4] * (1.0f / points[4].W);
            points[5] = points[5] * (1.0f / points[5].W);

            var ret = new Frustum {
                Origin = new Vector3(),
                Orientation = new Quaternion(),
                RightSlope = points[0].X,
                LeftSlope = points[1].X,
                TopSlope = points[2].Y,
                BottomSlope = points[3].Y,
                Near = points[4].Z,
                Far = points[5].Z
            };


            return ret;
        }

        public static Frustum Transform(Frustum frustum, float scale, Quaternion rotation, Vector3 translation) {
            var origin = frustum.Origin;
            var orientation = frustum.Orientation;

            orientation = orientation*rotation;
            Vector4 transform = Vector3.Transform(origin*scale, rotation);
            origin = new Vector3(transform.X, transform.Y, transform.Z) + translation;
            var ret = new Frustum {
                Origin = origin,
                Orientation = orientation,
                Near = frustum.Near*scale,
                Far = frustum.Far*scale,
                RightSlope = frustum.RightSlope,
                LeftSlope = frustum.LeftSlope,
                TopSlope = frustum.TopSlope,
                BottomSlope = frustum.BottomSlope
            };

            return ret;

        }
        // Return values: 0 = no intersection, 
        //                1 = intersection, 
        //                2 = box is completely inside frustum
        public int Intersect(BoundingBox boundingBox) {
            var planes = new[] {
                new Plane(0, 0, -1, Near),
                new Plane(0, 0, 1, Far),
                new Plane(1, 0, -RightSlope, 0),
                new Plane(-1, 0, LeftSlope, 0),
                new Plane(0, 1, -TopSlope, 0),
                new Plane(0, -1, BottomSlope, 0),
            };

            var origin = Origin;
            var orientation = Orientation;
            var totalIn = 0;
            var points = boundingBox.GetCorners().Select(s => Vector3.Transform(s - origin, Quaternion.Invert(orientation)).ToVector3()).ToArray();
            foreach (var plane in planes) {
                var inCount = planes.Length;
                var ptIn = 1;
                foreach (var point in points) {
                    if (Plane.DotCoordinate(plane, points[0]) < 0.0f) {
                        ptIn = 0;
                        inCount--;
                    }
                }
                if (inCount == 0) {
                    return 0;
                }
                totalIn += ptIn;
            }
            if (totalIn == 6) {
                return 2;
            }
            return 1;


        }
    }
}