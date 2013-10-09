namespace Core.Model {
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Assimp;

    using SlimDX;

    public class SceneAnimator {
        private Bone _skeleton;
        private readonly Dictionary<string, Bone> _bonesByName;
        private readonly Dictionary<string, int> _bonesToIndex;
        private readonly Dictionary<string, int> _animationNameToId;
        private readonly List<Bone> _bones;
        private List<Matrix> _transforms;
        public List<AnimEvaluator> Animations { get; private set; }
        public int CurrentAnimationIndex { get; private set; }
        public bool HasSkeleton { get { return _bones.Count > 0; } }
        public string AnimationName { get { return Animations[CurrentAnimationIndex].Name; } }
        public float AnimationSpeed { get { return Animations[CurrentAnimationIndex].TicksPerSecond; } }
        public float Duration {
            get { return Animations[CurrentAnimationIndex].Duration/ Animations[CurrentAnimationIndex].TicksPerSecond; }
        }

        public SceneAnimator() {
            _skeleton = null;
            CurrentAnimationIndex = -1;
            _bonesByName = new Dictionary<string, Bone>();
            _bonesToIndex = new Dictionary<string, int>();
            _animationNameToId = new Dictionary<string, int>();
            _bones = new List<Bone>();
            Animations = new List<AnimEvaluator>();
        }

        public void Init(Scene scene) {
            if (!scene.HasAnimations) {
                return;
            }
            Release();
            _skeleton = CreateBoneTree(scene.RootNode, null);


            foreach (var mesh in scene.Meshes) {
                foreach (var bone in mesh.Bones) {
                    Bone found;
                    if (!_bonesByName.TryGetValue(bone.Name, out found)) continue;

                    var skip = (from t in _bones let bname = bone.Name where t.Name == bname select t).Any();
                    if (skip) continue;

                    found.Offset = Matrix.Transpose(bone.OffsetMatrix.ToMatrix());
                    _bones.Add(found);
                    _bonesToIndex[found.Name] = _bones.IndexOf(found);
                }
                var mesh1 = mesh;
                foreach (var bone in _bonesByName.Keys.Where(b => mesh1.Bones.All(b1 => b1.Name != b) && b.StartsWith("Bone"))) {
                    _bonesByName[bone].Offset = _bonesByName[bone].Parent.Offset;
                    _bones.Add(_bonesByName[bone]);
                    _bonesToIndex[bone] = _bones.IndexOf(_bonesByName[bone]);
                }
            }
            ExtractAnimations(scene);
            _transforms = new List<Matrix>(Enumerable.Repeat(Matrix.Identity, _bones.Count));
            const float timestep = 1.0f / 60.0f;
            for (var i = 0; i < Animations.Count; i++) {
                SetAnimationIndex(i);
                var dt = 0.0f;
                for (var ticks = 0.0f; ticks < Animations[i].Duration; ticks += Animations[i].TicksPerSecond / 60.0f) {
                    dt += timestep;
                    Calculate(dt);
                    var trans = new List<Matrix>();
                    for (var a = 0; a < _transforms.Count; a++) {
                        var rotMat = _bones[a].Offset * _bones[a].GlobalTransform;
                        trans.Add(rotMat);
                    }
                    Animations[i].Transforms.Add(trans);
                }
                var distinctTrans = Animations[i].Transforms.Select(t => t.First()).Distinct().ToList();
                if (distinctTrans.Count == 1) {
                    Console.WriteLine("All transforms are the same");
                }
            }
            Console.WriteLine("Finished loading animations with " + _bones.Count + " bones");
        }

        private void Calculate(float dt) {
            if ((CurrentAnimationIndex < 0) | (CurrentAnimationIndex >= Animations.Count)) {
                return;
            }
            Animations[CurrentAnimationIndex].Evaluate(dt, _bonesByName);
            UpdateTransforms(_skeleton);
        }

        private static void UpdateTransforms(Bone node) {
            CalculateBoneToWorldTransform(node);
            foreach (var child in node.Children) {
                UpdateTransforms(child);
            }
        }

        private void ExtractAnimations(Scene scene) {
            foreach (var animation in scene.Animations) {
                Animations.Add(new AnimEvaluator(animation));
            }
            for (var i = 0; i < Animations.Count; i++) {
                _animationNameToId[Animations[i].Name] = i;
            }
            CurrentAnimationIndex = 0;
            SetAnimation("Idle");
        }

        private int _i;
        private Bone CreateBoneTree(Node node, Bone parent) {
            
            var internalNode = new Bone {
                Name = node.Name, Parent = parent
            };
            if (internalNode.Name == "") {
                internalNode.Name = "foo" + _i++;
            }

            _bonesByName[internalNode.Name] = internalNode;
            var trans = node.Transform;
            trans.Transpose();
            internalNode.LocalTransform = trans.ToMatrix();
            internalNode.OriginalLocalTransform = internalNode.LocalTransform;
            CalculateBoneToWorldTransform(internalNode);

            for (var i = 0; i < node.ChildCount; i++) {
                var child = CreateBoneTree(node.Children[i], internalNode);
                if (child != null) {
                    internalNode.Children.Add(child);
                }
            }
            return internalNode;
        }

        private static void CalculateBoneToWorldTransform(Bone child) {
            child.GlobalTransform = child.LocalTransform;
            var parent = child.Parent;
            while (parent != null) {
                child.GlobalTransform *= parent.LocalTransform;
                parent = parent.Parent;
            }
        }

        private void Release() {
            CurrentAnimationIndex = -1;
            Animations.Clear();
            _skeleton = null;
        }

        public bool SetAnimationIndex(int animIndex) {
            if (animIndex >= Animations.Count) {
                return false;
            }
            var oldIndex = CurrentAnimationIndex;
            CurrentAnimationIndex = animIndex;
            return oldIndex != CurrentAnimationIndex;
        }
        public bool SetAnimation(string animation) {
            int index;
            if (_animationNameToId.TryGetValue(animation, out index)) {
                var oldIndex = CurrentAnimationIndex;
                CurrentAnimationIndex = index;
                return oldIndex != CurrentAnimationIndex;
            }
            return false;
        }
        public void PlayAnimationForward() {
            Animations[CurrentAnimationIndex].PlayAnimationForward = true;
        }
        public void PlayAnimationBackward() {
            Animations[CurrentAnimationIndex].PlayAnimationForward = false;
        }
        public void AdjustAnimationSpeedBy(float prc) {
            Animations[CurrentAnimationIndex].TicksPerSecond *= prc / 100.0f;
        }
        public void AdjustAnimationSpeedTo(float ticksPerSec) {
            Animations[CurrentAnimationIndex].TicksPerSecond = ticksPerSec;
        }
        public List<Matrix> GetTransforms(float dt) {
            return Animations[CurrentAnimationIndex].GetTransforms(dt);
        }
        public List<Matrix> GetTransforms(int frame) {
            return Animations[CurrentAnimationIndex].Transforms[frame % Animations[CurrentAnimationIndex].Transforms.Count];
        }
        public int GetBoneIndex(string name) {
            
            if (_bonesToIndex.ContainsKey(name)) {

                return _bonesToIndex[name];
            }
            return -1;
        }
        public Matrix GetBoneTransform(float dt, string bname) {
            var i = GetBoneIndex(bname);
            if (i == -1) {
                return Matrix.Identity;
            }
            return Animations[CurrentAnimationIndex].GetTransforms(dt)[i];
        }
        public Matrix GetBoneTransform(float dt, int i) {
            return Animations[CurrentAnimationIndex].GetTransforms(dt)[i];
        }
    }
}