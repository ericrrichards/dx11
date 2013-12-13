#region

using System.Collections.Generic;
using System.Linq;

using Core.FX;

using SlimDX;
using SlimDX.Direct3D11;

#endregion

namespace Core.Model {
    using Core.Vertex;

    public class SkinnedModelInstance : IModelInstance<PosNormalTexTanSkinned> {
        private readonly Queue<string> _clipQueue = new Queue<string>();
        private readonly SkinnedModel _model;
        private string _clipName;
        private float _timePos;

        protected override void DrawNormalDepth(DeviceContext dc, EffectPass effectPass, Matrix view, Matrix proj) { throw new System.NotImplementedException(); }

        protected override void DrawShadowMap(DeviceContext dc, EffectPass effectPass, Matrix viewProj) { throw new System.NotImplementedException(); }

        protected override void DrawDisplacementMapped(DeviceContext dc, EffectPass effectPass, Matrix viewProj) { throw new System.NotImplementedException(); }

        protected override void DrawBasic(DeviceContext dc, EffectPass effectPass, Matrix viewProj) { throw new System.NotImplementedException(); }

        protected override void DrawNormalMapped(DeviceContext dc, EffectPass effectPass, Matrix viewProj) {
            var world = World;
            var wit = MathF.InverseTranspose(world);
            var wvp = world * viewProj;

            Effects.NormalMapFX.SetWorld(world);
            Effects.NormalMapFX.SetWorldInvTranspose(wit);
            Effects.NormalMapFX.SetWorldViewProj(wvp);
            Effects.NormalMapFX.SetTexTransform(Matrix.Identity);

            Effects.NormalMapFX.SetBoneTransforms(FinalTransforms);

            for (var i = 0; i < Model.SubsetCount; i++) {
                Effects.NormalMapFX.SetMaterial(Model.Materials[i]);
                Effects.NormalMapFX.SetDiffuseMap(Model.DiffuseMapSRV[i]);
                Effects.NormalMapFX.SetNormalMap(Model.NormalMapSRV[i]);

                effectPass.Apply(dc);
                Model.ModelMesh.Draw(dc, i);
            }
        }

        public string ClipName {
            get { return _clipName; }
            set {
                _clipName = _model.Animator.Animations.Any(a => a.Name == value) ? value : "Still";
                _model.Animator.SetAnimation(_clipName);
                _timePos = 0;
            }
        }

        // these are the available animation clips
        public IEnumerable<string> Clips { get { return _model.Animator.Animations.Select(a => a.Name); } }

        public bool LoopClips { get; set; }

        // the bone transforms for the mesh instance
        private List<Matrix> FinalTransforms { get { return _model.Animator.GetTransforms(_timePos); } }

        public SkinnedModelInstance(string clipName, Matrix transform, SkinnedModel model) : base(model) {
            World = transform;
            _model = (SkinnedModel)Model;
            ClipName = clipName;
        }

        public void Update(float dt) {
            _timePos += dt;

            if (_timePos > _model.Animator.Duration) {
                if (_clipQueue.Any()) {
                    ClipName = _clipQueue.Dequeue();
                    if (LoopClips) {
                        _clipQueue.Enqueue(ClipName);
                    }
                } else {
                    ClipName = "Still";
                }
            }
        }

        public void AddClip(string clip) {
            if (_model.Animator.Animations.Any(a => a.Name == clip)) {
                _clipQueue.Enqueue(clip);
            }
        }

        public void ClearClips() { _clipQueue.Clear(); }

    }
}
