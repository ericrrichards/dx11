using System;
using System.Collections.Generic;
using System.Linq;
using SlimDX;

namespace Core.Model {
    class BoneAnimation {
        private List<Keyframe> _keyframes;
        internal void SetKeyFrames(List<Keyframe> frames) {
            _keyframes = frames;
        }


        public float StartTime { get { return _keyframes.First().TimePos; } }
        public float EndTime { get { return _keyframes.Last().TimePos; } }
        public Matrix Interpolate(float t) {
            if (t <= _keyframes.First().TimePos) {
                var s = _keyframes.First().Scale;
                var p = _keyframes.First().Translation;
                var q = _keyframes.First().RotationQuat;

                return Matrix.RotationQuaternion(q) * Matrix.Scaling(s) * Matrix.Translation(p);
            }
            if (t >= _keyframes.Last().TimePos) {
                var s = _keyframes.Last().Scale;
                var p = _keyframes.Last().Translation;
                var q = _keyframes.Last().RotationQuat;

                return Matrix.RotationQuaternion(q) * Matrix.Scaling(s) * Matrix.Translation(p);
            }
            for (var i = 0; i < _keyframes.Count - 1; i++) {
                var k0 = _keyframes[i];
                var k1 = _keyframes[i + 1];
                if (t >= k0.TimePos && t <= k1.TimePos) {
                    var lerpPercent = (t - k0.TimePos) / (k1.TimePos - k0.TimePos);
                    var s0 = k0.Scale;
                    var s1 = k1.Scale;

                    var p0 = k0.Translation;
                    var p1 = k1.Translation;

                    var q0 = k0.RotationQuat;
                    var q1 = k1.RotationQuat;

                    var s = Vector3.Lerp(s0, s1, lerpPercent);
                    var p = Vector3.Lerp(p0, p1, lerpPercent);
                    var q = Quaternion.Lerp(q0, q1, lerpPercent);

                    return Matrix.RotationQuaternion(q) * Matrix.Scaling(s) * Matrix.Translation(p);
                }
            }
            // shouldn't get here
            throw new Exception("failed to find appropriate keyframe");
        }
    }
}