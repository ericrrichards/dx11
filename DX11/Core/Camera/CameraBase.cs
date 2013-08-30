using SlimDX;

namespace Core.Camera {
    public abstract class CameraBase {
        protected Frustum _frustum;
        public Vector3 Position { get; set; }
        public Vector3 Right { get; protected set; }
        public Vector3 Up { get; protected set; }
        public Vector3 Look { get; protected set; }
        public float NearZ { get; protected set; }
        public float FarZ { get; protected set; }
        public float Aspect { get; protected set; }
        public float FovY { get; protected set; }
        public float FovX {
            get {
                var halfWidth = 0.5f * NearWindowWidth;
                return 2.0f * MathF.Atan(halfWidth / NearZ);
            }
        }
        public float NearWindowWidth { get { return Aspect * NearWindowHeight; } }
        public float NearWindowHeight { get; protected set; }
        public float FarWindowWidth { get { return Aspect * FarWindowHeight; } }
        public float FarWindowHeight { get; protected set; }
        public Matrix View { get; protected set; }
        public Matrix Proj { get; protected set; }
        public Matrix ViewProj { get { return View * Proj; } }

        protected CameraBase() {
            Position = new Vector3();
            Right = new Vector3(1, 0, 0);
            Up = new Vector3(0, 1, 0);
            Look = new Vector3(0, 0, 1);

            View = Matrix.Identity;
            Proj = Matrix.Identity;
            SetLens(0.25f * MathF.PI, 1.0f, 1.0f, 1000.0f);
        }

        public abstract void LookAt(Vector3 pos, Vector3 target, Vector3 up);
        public abstract void Strafe(float d);
        public abstract void Walk(float d);
        public abstract void Pitch(float angle);
        public abstract void Yaw(float angle);
        public abstract void Zoom(float dr);
        public abstract void UpdateViewMatrix();

        public bool Visible(BoundingBox box) {
            return _frustum.Intersect(box) > 0;
        }

        public void SetLens(float fovY, float aspect, float zn, float zf) {
            FovY = fovY;
            Aspect = aspect;
            NearZ = zn;
            FarZ = zf;

            NearWindowHeight = 2.0f * NearZ * MathF.Tan(0.5f * FovY);
            FarWindowHeight = 2.0f * FarZ * MathF.Tan(0.5f * FovY);

            Proj = Matrix.PerspectiveFovLH(FovY, Aspect, NearZ, FarZ);
        }


        public Ray GetPickingRay(Matrix objWorld, Vector2 sp, Vector2 screenDims) {
            var p = Proj;

            var vx = (2.0f * sp.X / screenDims.X - 1.0f) / p.M11;
            var vy = (-2.0f * sp.Y / screenDims.Y + 1.0f) / p.M22;

            var ray = new Ray(new Vector3(), new Vector3(vx, vy, 1.0f));
            var v = View;
            var invView = Matrix.Invert(v);

            var w = objWorld;
            var invWorld = Matrix.Invert(w);

            var toLocal = invView * invWorld;

            ray = new Ray(Vector3.TransformCoordinate(ray.Position, toLocal), Vector3.TransformNormal(ray.Direction, toLocal));

            ray.Direction.Normalize();
            return ray;
        }
    }
}