using SlimDX;

namespace Core.Camera {
    public class LookAtCamera  : CameraBase {
        private Vector3 _target;
        private float _radius;
        private float _alpha;
        private float _beta;

        public LookAtCamera() {
            _alpha = _beta = 0.5f;
            _radius = 10.0f;
            _target = new Vector3();

        }

        public override void LookAt(Vector3 pos, Vector3 target, Vector3 up) {
            _target = target;
            Position = pos;
            Position = pos;
            Look = Vector3.Normalize(target - pos);
            Right = Vector3.Normalize(Vector3.Cross(up, Look));
            Up = Vector3.Cross(Look, Right);
            _radius = (target - pos).Length();
        }

        public override void Strafe(float d) {
            var dt = Vector3.Normalize(new Vector3(Right.X, 0, Right.Z)) * d;
            _target += dt;
        }

        public override void Walk(float d) {
            _target += Vector3.Normalize(new Vector3(Look.X, 0, Look.Z)) *d;
        }

        public override void Pitch(float angle) {
            _beta += angle;
            _beta = MathF.Clamp(_beta, 0.05f, MathF.PI/2.0f - 0.01f);
        }

        public override void Yaw(float angle) {
            _alpha = (_alpha + angle) % (MathF.PI*2.0f);
        }
        public override void Zoom(float dr) {
            _radius += dr;
            _radius = MathF.Clamp(_radius, 2.0f, 70.0f);
        }

        public override void UpdateViewMatrix() {
            var sideRadius = _radius*MathF.Cos(_beta);
            var height = _radius*MathF.Sin(_beta);

            Position = new Vector3(
                _target.X + sideRadius * MathF.Cos(_alpha), 
                _target.Y + height, 
                _target.Z + sideRadius * MathF.Sin(_alpha) 
            );

            View = Matrix.LookAtLH(Position, _target, Vector3.UnitY);

            Right = new Vector3(View.M11, View.M21, View.M31);
            Right.Normalize();

            Look = new Vector3(View.M13, View.M23, View.M33);
            Look.Normalize();
        }
    }
}