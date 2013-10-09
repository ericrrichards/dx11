using System.Collections.Generic;
using System.Linq;
using SlimDX;

namespace Core.Model {
    public class SkinnedModelInstance {
        public readonly SkinnedModel Model;
        public float TimePos;
        public string ClipName {
            get { return _clipName; }
            set {
                if (Model.Animator.Animations.Any(a => a.Name == value)) {
                    _clipName = value;
                } else {
                    _clipName = "Still";
                }
                Model.Animator.SetAnimation(_clipName);
                TimePos = 0;

            }
        }

        public IEnumerable<string> Clips { get { return Model.Animator.Animations.Select(a => a.Name); } } 
        private readonly Queue<string> _clipQueue = new Queue<string>();

        public Matrix World;
        public List<Matrix> FinalTransforms = new List<Matrix>();
        private string _clipName;
        public bool LoopClips { get; set; }

        public SkinnedModelInstance(string clipName, Matrix transform, SkinnedModel model) {

            World = transform;
            Model = model;

            ClipName = clipName;

        }

        public void Update(float dt) {
            TimePos += dt;


            var d = Model.Animator.Duration;
            if (TimePos > d) {
                if (_clipQueue.Any()) {
                    ClipName = _clipQueue.Dequeue();
                    if (LoopClips) {
                        _clipQueue.Enqueue(ClipName);
                    }
                } else {
                    ClipName = "Still";
                }
            }

            FinalTransforms = Model.Animator.GetTransforms(TimePos);

        }
        public void AddClip(string clip) {
            if (Model.Animator.Animations.Any(a => a.Name == clip)) {
                _clipQueue.Enqueue(clip);
            }
        }
        public void ClearClips() {
            _clipQueue.Clear();
        }
    }
}