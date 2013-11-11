namespace Minimap {
    using Core;
    using Core.Camera;

    using SlimDX;

    internal class OrthoCamera : CameraBase {
        public Vector3 Target { get; set; }
        public override void LookAt(Vector3 pos, Vector3 target, Vector3 up) {
            Position = pos;
            Look = Vector3.Normalize(target - pos);
            Right = Vector3.Normalize(Vector3.Cross(up, Look));
            Up = Vector3.Cross(Look, Right);
        }

        public override void Strafe(float d) {
            Position += Right * d;
        }

        public override void Walk(float d) {
            Position += Look * d;
        }

        public override void Pitch(float angle) {
            var r = Matrix.RotationAxis(Right, angle);
            Up = Vector3.TransformNormal(Up, r);
            Look = Vector3.TransformNormal(Look, r);
        }

        public override void Yaw(float angle) {
            var r = Matrix.RotationY(angle);
            Right = Vector3.TransformNormal(Right, r);
            Up = Vector3.TransformNormal(Up, r);
            Look = Vector3.TransformNormal(Look, r);
        }

        public override void Zoom(float dr) {
            Walk(dr);
        }

        public override void UpdateViewMatrix() {
            View = Matrix.LookAtLH(Position, Target, new Vector3(0, 0, 1));

            _frustum = Frustum.FromViewProj(ViewProj);
        }
        public void SetLens(float width, float height) {
            Proj = Matrix.OrthoLH(width, height, 0.1f, 2000);
            UpdateViewMatrix();
        }
        public OrthoCamera() {
            Target = new Vector3();
            Up = new Vector3(0, 0, 1);
            Look = new Vector3(0, -1, 0);
            SetLens(100, 100);
            UpdateViewMatrix();
        }
    }
}