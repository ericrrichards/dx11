namespace Core.Model {
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Assimp;

    using SlimDX;

    public class SceneAnimator {
        protected Bone Skeleton;
        protected readonly Dictionary<string, Bone> BonesByName;
        protected readonly Dictionary<string, int> BonesToIndex;
        protected readonly Dictionary<string, int> AnimationNameToId;
        protected readonly List<Bone> Bones;
        protected List<Matrix> Transforms;
        public List<AnimEvaluator> Animations { get; private set; } 
        public int CurrentAnimationIndex { get; private set; }
        public bool HasSkeleton { get { return Bones.Count > 0; } }
        public string AnimationName { get { return Animations[CurrentAnimationIndex].Name; } }
        public float AnimationSpeed { get { return Animations[CurrentAnimationIndex].TicksPerSecond; } }

        public SceneAnimator() {
            Skeleton = null;
            CurrentAnimationIndex = -1;
            BonesByName = new Dictionary<string, Bone>();
            BonesToIndex = new Dictionary<string, int>();
            AnimationNameToId = new Dictionary<string, int>();
            Bones = new List<Bone>();
            Animations = new List<AnimEvaluator>();
        }

        public void Init(Scene scene) {
            if (!scene.HasAnimations) {
                return;
            }
            Release();
            Skeleton = CreateBoneTree(scene.RootNode, null);
            ExtractAnimations(scene);

            foreach (var mesh in scene.Meshes) {
                foreach (var bone in mesh.Bones) {
                    Bone found;
                    if ( BonesByName.TryGetValue(bone.Name, out found)){
                        var skip = (from t in Bones let bname = bone.Name where t.Name == bname select t).Any();
                        if (!skip) {
                            var tes = found.Name;
                            found.Offset = bone.OffsetMatrix.ToMatrix();
                            Bones.Add(found);
                            BonesToIndex[found.Name] = Bones.IndexOf(found);
                        }
                    }
                }
            }
            Transforms = new List<Matrix>(Enumerable.Repeat(Matrix.Identity, Bones.Count));
            const float timestep = 1.0f / 30.0f;
            for (int i = 0; i < Animations.Count; i++) {
                SetAnimationIndex(i);
                var dt = 0.0f;
                for (var ticks = 0.0f; ticks < Animations[i].Duration; ticks+=Animations[i].TicksPerSecond/30.0f) {
                    dt += timestep;
                    Calculate(dt);
                    Animations[i].Transforms.Add(new List<Matrix>());
                    var trans = Animations[i].Transforms.Last();
                    for (int a = 0; a < Transforms.Count; a++) {
                        var rotMat = Bones[a].Offset * Bones[a].GlobalTransform;
                        trans.Add(rotMat);
                    }
                }
            }
            Console.WriteLine("Finished loading animations with " + Bones.Count + " bones");
        }

        private void Calculate(float dt) {
            if ((CurrentAnimationIndex < 0) | (CurrentAnimationIndex >= Animations.Count)) {
                return;
            }
            Animations[CurrentAnimationIndex].Evaluate(dt, BonesByName);
            UpdateTransforms(Skeleton);
        }

        private void UpdateTransforms(Bone node) {
            CalculateBoneToWorldTransform(node);
            foreach (var child in node.Children) {
                UpdateTransforms(child);
            }
        }

        private void ExtractAnimations(Scene scene) {
            foreach (var animation in scene.Animations) {
                Animations.Add(new AnimEvaluator(animation));
            }
            for (int i = 0; i < Animations.Count; i++) {
                AnimationNameToId[Animations[i].Name] = i;
            }
            CurrentAnimationIndex = 0;
            SetAnimation("Idle");
        }

        private Bone CreateBoneTree(Node node, Bone parent) {
            var internalNode = new Bone {
                Name = node.Name, Parent = parent
            };

            BonesByName[internalNode.Name] = internalNode;
            internalNode.LocalTransform = node.Transform.ToMatrix();
            internalNode.LocalTransform = Matrix.Transpose(internalNode.LocalTransform);
            internalNode.OriginalLocalTransform = internalNode.LocalTransform;
            CalculateBoneToWorldTransform(internalNode);

            for (var i = 0; i < node.ChildCount; i++) {
                internalNode.Children.Add(CreateBoneTree(node.Children[i], internalNode));
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
            Skeleton = null;
        }

        public bool SetAnimationIndex(int animIndex) {
            if ( animIndex >= Animations.Count ) {
                return false;
            }
            var oldIndex = CurrentAnimationIndex;
            CurrentAnimationIndex = animIndex;
            return oldIndex != CurrentAnimationIndex;
        }
        public bool SetAnimation(string animation) {
            int index;
            if (AnimationNameToId.TryGetValue(animation, out index)) {
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
        public int GetBoneIndex(string name) {
            if (BonesToIndex.ContainsKey(name)) {
                return BonesToIndex[name];
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
    public class Bone {
        public string Name { get; set; }
        public Matrix Offset { get; set; }
        public Matrix LocalTransform { get; set; }
        public Matrix GlobalTransform { get; set; }
        public Matrix OriginalLocalTransform { get; set; }
        public Bone Parent { get; set; }
        public List<Bone> Children { get; set; }
        public Bone() {
            Children = new List<Bone>();
        }
    }
}