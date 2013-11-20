using SlimDX;

namespace Core.Camera {
    using System.Security.Cryptography;

    public abstract class CameraBase {
        public HeightFunc Height { get; set; }
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
        public Plane[] FrustumPlanes {get { return _frustum.Planes; } }

        protected CameraBase() {
            Position = new Vector3();
            Right = new Vector3(1, 0, 0);
            Up = new Vector3(0, 1, 0);
            Look = new Vector3(0, 0, 1);

            View = Matrix.Identity;
            Proj = Matrix.Identity;
            //SetLens(0.25f * MathF.PI, 1.0f, 1.0f, 1000.0f);
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

        public virtual void SetLens(float fovY, float aspect, float zn, float zf) {
            FovY = fovY;
            Aspect = aspect;
            NearZ = zn;
            FarZ = zf;

            NearWindowHeight = 2.0f * NearZ * MathF.Tan(0.5f * FovY);
            FarWindowHeight = 2.0f * FarZ * MathF.Tan(0.5f * FovY);

            Proj = Matrix.PerspectiveFovLH(FovY, Aspect, NearZ, FarZ);
        }

        /// <summary>
        /// Return picking ray from camera through sp on screen, in world-space
        /// </summary>
        /// <param name="sp"></param>
        /// <param name="screenDims"></param>
        /// <returns></returns>
        public Ray GetPickingRay(Vector2 sp, Vector2 screenDims) {
            var p = Proj;
            // convert screen pixel to view space
            var vx = (2.0f * sp.X / screenDims.X - 1.0f) / p.M11;
            var vy = (-2.0f * sp.Y / screenDims.Y + 1.0f) / p.M22;

            var ray = new Ray(new Vector3(), new Vector3(vx, vy, 1.0f));
            var v = View;
            var invView = Matrix.Invert(v);


            var toWorld = invView;

            ray = new Ray(Vector3.TransformCoordinate(ray.Position, toWorld), Vector3.TransformNormal(ray.Direction, toWorld));

            ray.Direction.Normalize();
            return ray;
        }

        public Vector3[] GetFrustumCorners() {
            var hNear = 2 * MathF.Tan(FovY / 2) * NearZ;
            var wNear = hNear * Aspect;

            var hFar = 2 * MathF.Tan(FovY / 2) * FarZ;
            var wFar = hFar * Aspect;

            var cNear = Position + Look * NearZ;
            var cFar = Position + Look * FarZ;

            return new[] {
                //ntl
                cNear + (Up*hNear/2) - (Right*wNear/2),
                //ntr
                cNear + (Up*hNear/2) + (Right*wNear/2),
                //nbl
                cNear - (Up *hNear/2) - (Right*wNear/2),
                //nbr
                cNear - (Up *hNear/2) + (Right*wNear/2),
                //ftl
                cFar + (Up*hFar/2) - (Right*wFar/2),
                //ftr
                cFar + (Up*hFar/2) + (Right*wFar/2),
                //fbl
                cFar - (Up *hFar/2) - (Right*wFar/2),
                //fbr
                cFar - (Up *hFar/2) + (Right*wFar/2),
            };

        }
    }
}