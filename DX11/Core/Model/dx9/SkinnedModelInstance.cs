namespace Core.Model.dx9 {
    using System.Collections.Generic;
    using System.Linq;

    using SlimDX;
    using SlimDX.Direct3D9;

    public class SkinnedModelInstance {
        public SkinnedModel9 Model;
        public float TimePos;
        private int frame = 0;
        public string ClipName { get { return _clipName; } set {
            if (_clipName != value) {
                _clipName = value;
                _animationController.SetTrackAnimationSet(0, _animationController.GetAnimationSet<AnimationSet>(Animations[_clipName]));
            }
        } }
        public Matrix World;
        public List<Matrix> FinalTransforms = new List<Matrix>();
        private AnimationController _animationController;
        private Dictionary<string, int> Animations;
        private string _clipName;

        public SkinnedModelInstance(SkinnedModel9 model) {
            Model = model;
            _animationController = model.GetAnimationControllerClone();
            Animations = model.Animations;
        }
        public void Update(float dt) {
            Model.Update(dt, _animationController);
            FinalTransforms = Model.GetBoneMatrices().ToList();
        }
    }
}