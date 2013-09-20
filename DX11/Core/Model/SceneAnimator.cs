using System.Drawing;
using Core.Camera;
using Core.FX;
using Core.Vertex;
using SlimDX.Direct3D11;

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
        protected List<Matrix> _transforms;
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


            foreach (var mesh in scene.Meshes) {
                foreach (var bone in mesh.Bones) {
                    Bone found;
                    if (BonesByName.TryGetValue(bone.Name, out found)) {
                        var skip = (from t in Bones let bname = bone.Name where t.Name == bname select t).Any();
                        if (!skip) {
                            var tes = found.Name;
                            found.Offset = Matrix.Transpose(bone.OffsetMatrix.ToMatrix());
                            Bones.Add(found);
                            BonesToIndex[found.Name] = Bones.IndexOf(found);
                        }
                    }
                }
                foreach (var bone in BonesByName.Keys.Where(b => !mesh.Bones.Any(b1 => b1.Name == b) && b.StartsWith("Bone"))) {
                    BonesByName[bone].Offset = BonesByName[bone].Parent.Offset;
                    Bones.Add(BonesByName[bone]);
                    BonesToIndex[bone] = Bones.IndexOf(BonesByName[bone]);
                }
            }
            ExtractAnimations(scene);
            _transforms = new List<Matrix>(Enumerable.Repeat(Matrix.Identity, Bones.Count));
            const float timestep = 1.0f / 30.0f;
            for (int i = 0; i < Animations.Count; i++) {
                SetAnimationIndex(i);
                var dt = 0.0f;
                for (var ticks = 0.0f; ticks < Animations[i].Duration; ticks += Animations[i].TicksPerSecond / 30.0f) {
                    dt += timestep;
                    Calculate(dt);
                    var trans = new List<Matrix>();
                    for (int a = 0; a < _transforms.Count; a++) {
                        var rotMat = Bones[a].Offset * Bones[a].GlobalTransform;
                        trans.Add(rotMat);
                    }
                    Animations[i].Transforms.Add(trans);
                }
                var distinctTrans = Animations[i].Transforms.Select(t => t.First()).Distinct().ToList();
                if (distinctTrans.Count == 1) {
                    Console.WriteLine("All transforms are the same");
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
                Animations.Add(new AnimEvaluator(animation, this));
            }
            for (int i = 0; i < Animations.Count; i++) {
                AnimationNameToId[Animations[i].Name] = i;
            }
            CurrentAnimationIndex = 0;
            SetAnimation("Idle");
        }

        private int _i;
        private Bone CreateBoneTree(Node node, Bone parent) {
            /*if (string.IsNullOrEmpty(node.Name)) {
                return null;
            }*/
            var internalNode = new Bone {
                Name = node.Name, Parent = parent
            };
            if (internalNode.Name == "") {
                internalNode.Name = "foo" + _i++;
            }

            BonesByName[internalNode.Name] = internalNode;
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
            Skeleton = null;
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
        public List<Matrix> GetTransforms(int frame) {
            return Animations[CurrentAnimationIndex].Transforms[frame % Animations[CurrentAnimationIndex].Transforms.Count];
        }
        public int GetBoneIndex(string name) {
            /*if (BonesByName[name].Children.Count(c=>c.Name!="") == 1) {
                return BonesToIndex[BonesByName[name].Children.First(c=>c.Name != "").Name];
            }*/
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

        private static Color[] boneColors = {
            Color.Black,
            Color.Red,
            Color.Blue,
            Color.Green,
            Color.Yellow,
            Color.Magenta,
            Color.Cyan,
            Color.White,
            Color.BlueViolet,
            Color.YellowGreen,
            Color.Brown,
            Color.CadetBlue,
            Color.SandyBrown,
            Color.Tomato,
            Color.SpringGreen,
            Color.Sienna,
            Color.Thistle,
            Color.CornflowerBlue
        };

        public void RenderSkeleton(DeviceContext dc, CameraBase camera, Bone bone, Bone parent, Matrix world) {
            if (bone == null) {
                bone = Bones.First(b => b.Parent.Parent == null);
            }
            if (parent != null && !string.IsNullOrEmpty(bone.Name) && !string.IsNullOrEmpty(parent.Name)) {

                var w1 = Animations[CurrentAnimationIndex].GetTransforms(0)[BonesToIndex[bone.Name]];
                var w2 = Animations[CurrentAnimationIndex].GetTransforms(0)[BonesToIndex[parent.Name]];
                var thisBone = new Vector3(w1[3, 0], w1[3, 1], w1[3, 2]);
                var parentBone = new Vector3(w2[3, 0], w2[3, 1], w2[3, 2]);
                Console.WriteLine("this: {0} - {1}", bone.Name, thisBone);
                Console.WriteLine("parent: {0} - {1}", parent.Name, parentBone);

                var vb = new SlimDX.Direct3D11.Buffer(dc.Device,
                    new DataStream(new[] {
                        new VertexPC(parentBone, boneColors[BonesToIndex[parent.Name] % boneColors.Length]), 
                        new VertexPC(thisBone, boneColors[BonesToIndex[bone.Name] % boneColors.Length]),
                    }, false, false),
                    new BufferDescription(
                        VertexPC.Stride * 2,
                        ResourceUsage.Immutable,
                        BindFlags.VertexBuffer,
                        CpuAccessFlags.None,
                        ResourceOptionFlags.None,
                        0));

                //if (Vector3.Distance(thisBone , parentBone) < 20.0f) {
                Effects.ColorFX.SetWorldViewProj(world * camera.ViewProj);
                dc.InputAssembler.InputLayout = InputLayouts.PosColor;
                dc.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineList;
                dc.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vb, VertexPC.Stride, 0));
                for (int p = 0; p < Effects.ColorFX.ColorTech.Description.PassCount; p++) {
                    Effects.ColorFX.ColorTech.GetPassByIndex(p).Apply(dc);
                    dc.Draw(2, 0);
                }
                //}
                Util.ReleaseCom(ref vb);

            }
            foreach (var child in bone.Children) {
                RenderSkeleton(dc, camera, child, bone, world);
            }

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