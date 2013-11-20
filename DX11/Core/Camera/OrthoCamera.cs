namespace Core.Camera {
    using Core;

    using SlimDX;

public class OrthoCamera : CameraBase {
    public Vector3 Target { get; set; }

    public OrthoCamera() {
        Target = new Vector3();
        Up = new Vector3(0, 0, 1);
        Look = new Vector3(0, -1, 0);      
    }
    public override void UpdateViewMatrix() {
        View = Matrix.LookAtLH(Position, Target, new Vector3(0, 0, 1));

        _frustum = Frustum.FromViewProj(ViewProj);
    }

    public override void SetLens(float width, float height, float znear, float zfar) {
        Proj = Matrix.OrthoLH(width, height, 0.1f, 2000);
        UpdateViewMatrix();
    }
    public override void LookAt(Vector3 pos, Vector3 target, Vector3 up) { }
    public override void Strafe(float d) { }
    public override void Walk(float d) { }
    public override void Pitch(float angle) { }
    public override void Yaw(float angle) { }
    public override void Zoom(float dr) { }
}
}