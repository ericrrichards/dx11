using SlimDX;

namespace Core.Model {
    class Keyframe {
        public Keyframe() {
            TimePos = 0.0f;
            Translation = new Vector3(0);
            Scale = new Vector3(1);
            RotationQuat = Quaternion.Identity;
        }

        internal float TimePos;
        internal Vector3 Translation;
        internal Vector3 Scale;
        internal Quaternion RotationQuat;
    }
}