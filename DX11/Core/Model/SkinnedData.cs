using System.Collections.Generic;
using System.Linq;
using SlimDX;

namespace Core.Model {
    using Assimp;

    public class SkinnedData {
        private List<int> _boneHierarchy;
        private List<Matrix> _boneOffsets;
        private Dictionary<string, AnimationClip> _animations;

        public float GetClipEndTime(string clipName) {
            return _animations[clipName].ClipEndTime;
        }
        public float GetClipStartTime(string clipName) {
            return _animations[clipName].ClipStartTime;
        }
        public int BoneCount { get { return _boneHierarchy.Count; } }
        public void Set(List<int> boneHierarchy, List<Matrix> boneOffsets, Dictionary<string, AnimationClip> animations) {
            _boneHierarchy = boneHierarchy;
            _boneOffsets = boneOffsets;
            _animations = animations;
        }
        public List<Matrix> GetFinalTransforms(string clipName, float timePos) {
            var numBones = _boneOffsets.Count;

            var finalTransforms = new Matrix[numBones];

            var clip = _animations[clipName];
            var toParentTransforms = clip.Interpolate(timePos);
            var toRootTransforms = new Matrix[numBones];
            toRootTransforms[0] = toParentTransforms[0];

            for (int i = 1; i < numBones; i++) {
                var toParent = toParentTransforms[i];
                var parentIndex = _boneHierarchy[i];
                var parentToRoot = toRootTransforms[parentIndex];
                var toRoot = toParent * parentToRoot;
                toRootTransforms[i] = toRoot;
            }
            for (int i = 0; i < numBones; i++) {
                var offset = _boneOffsets[i];
                var toRoot = toRootTransforms[i];
                finalTransforms[i] = offset * toRoot;
            }
            return finalTransforms.ToList();
        }
    }

    
}