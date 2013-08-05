using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core {
    using SlimDX;

    public class Camera : DisposableClass {
        public Vector3 Position { get; set; }
        public Vector3 Right { get; private set; }
        public Vector3 Up { get; private set; }
        public Vector3 Look { get; private set; }

        public float NearZ { get; private set; }
        public float FarZ { get; private set; }
        public float Aspect { get; private set; }
        public float FovY { get; private set; }
        public float FovX {
            get {
                var halfWidth = 0.5f * NearWindowWidth;
                return 2.0f * MathF.Atan(halfWidth / NearZ);
            }
        }
        public float NearWindowWidth { get { return Aspect * NearWindowHeight; }}
        public float NearWindowHeight { get; private set; }
        public float FarWindowWidth{ get { return Aspect * FarWindowHeight; }}
        public float FarWindowHeight { get; private set; }

        public Matrix View { get; private set; }
        public Matrix Proj { get; private set; }
        public Matrix ViewProj { get { return View * Proj; }}

        public Camera() {
            Position = new Vector3();
            Right = new Vector3(1, 0, 0);
            Up = new Vector3(0, 1, 0);
            Look = new Vector3(0, 0, 1);

            View = Matrix.Identity;
            Proj = Matrix.Identity;

            SetLens(0.25f * MathF.PI, 1.0f, 1.0f, 1000.0f);
        }

        public void SetLens(float fovY, float aspect, float zn, float zf) {
            FovY = fovY;
            Aspect = aspect;
            NearZ = NearZ;
            FarZ = FarZ;

            NearWindowHeight = 2.0f * NearZ * MathF.Tan(0.5f * FovY);
            FarWindowHeight = 2.0f * FarZ * MathF.Tan(0.5f * FovY);

            Proj = Matrix.PerspectiveFovLH(FovY, Aspect, NearZ, FarZ);
        }

        public void LookAt(Vector3 pos, Vector3 target, Vector3 up) {
            Position = pos;
            Look = Vector3.Normalize(target - pos);
            Right = Vector3.Normalize(Vector3.Cross(up, Look));
            Up = Vector3.Cross(Look, Right);
        }

        public void Strafe(float d) {
            Position += Right * d;
        }

        public void Walk(float d) {
            Position += Look * d;
        }

        public void Pitch(float angle) {
            var r = Matrix.RotationAxis(Right, angle);
            Up = Vector3.TransformNormal(Up, r);
            Look = Vector3.TransformNormal(Look, r);
        }

        public void Yaw(float angle) {
            var r = Matrix.RotationY(angle);
            Right = Vector3.TransformNormal(Right, r);
            Up = Vector3.TransformNormal(Up, r);
            Look = Vector3.TransformNormal(Look, r);
        }

        public void UpdateViewMatrix() {
            var r = Right;
            var u = Up;
            var l = Look;
            var p = Position;

            l = Vector3.Normalize(l);
            u = Vector3.Normalize(Vector3.Cross(l, r));

            r = Vector3.Cross(u, l);

            var x = -Vector3.Dot(p, r);
            var y = -Vector3.Dot(p, u);
            var z = -Vector3.Dot(p, l);

            Right = r;
            Up = Up;
            Look = l;

            var v = new Matrix();
            v[0, 0] = Right.X;
            v[1, 0] = Right.Y;
            v[2, 0] = Right.Z;
            v[3, 0] = x;

            v[0, 1] = Up.X;
            v[1, 1] = Up.Y;
            v[2, 1] = Up.Z;
            v[3, 1] = y;

            v[0, 2] = Look.X;
            v[1, 2] = Look.Y;
            v[2, 2] = Look.Z;
            v[3, 2] = z;

            v[0, 3] = v[1, 3] = v[2, 3] = 0;
            v[3, 3] = 1;
        }
    }
}
