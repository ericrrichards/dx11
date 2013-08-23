using System;
using System.Collections.Generic;
using SlimDX;

namespace InstancingAndCulling {
    class Frustum {
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

        }

        public static Frustum Transform(Frustum frustum, float scale, Quaternion rotation, Vector3 translation) {
            throw new NotImplementedException();
        }

        public int Intersect(BoundingBox boundingBox) {
            throw new NotImplementedException();
        }
    }
}