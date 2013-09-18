using System;
using System.Collections.Generic;
using System.Linq;
using SlimDX;

namespace Core.Model {
    public class AnimationClip {
        private List<BoneAnimation> _boneAnimations;
        internal void SetBoneAnimations(List<BoneAnimation> boneAnims) {
            _boneAnimations = boneAnims;
        }

        public float ClipStartTime {
            get {
                var t = _boneAnimations.Select(t1 => t1.StartTime).Concat(new[] { float.PositiveInfinity }).Min();
                return t;
            }
        }

        public float ClipEndTime {
            get {
                var t = 0.0f;
                foreach (var boneAnimation in _boneAnimations) {
                    t = Math.Max(t, boneAnimation.EndTime);
                }
                return t;
            }
        }
        public List<Matrix> Interpolate(float t) {
            var ret = _boneAnimations.Select(ba => ba.Interpolate(t)).ToList();
            return ret;
        }
    }
}