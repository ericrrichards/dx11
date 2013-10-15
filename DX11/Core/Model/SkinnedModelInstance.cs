using System.Collections.Generic;
using System.Linq;
using Core.FX;
using SlimDX;
using SlimDX.Direct3D11;

namespace Core.Model {
    public class SkinnedModelInstance {
        private readonly SkinnedModel _model;
        private float _timePos;
        public Matrix World { get; set; }
        public string ClipName {
            get { return _clipName; }
            set {
                _clipName = _model.Animator.Animations.Any(a => a.Name == value) ? value : "Still";
                _model.Animator.SetAnimation(_clipName);
                _timePos = 0;
            }
        }
        private string _clipName;

        // these are the available animation clips
        public IEnumerable<string> Clips { get { return _model.Animator.Animations.Select(a => a.Name); } } 

        private readonly Queue<string> _clipQueue = new Queue<string>();
        public bool LoopClips { get; set; }
        
        // the bone transforms for the mesh instance
        private List<Matrix> FinalTransforms  { get { return _model.Animator.GetTransforms(_timePos); }}
        
        public SkinnedModelInstance(string clipName, Matrix transform, SkinnedModel model) {
            World = transform;
            _model = model;
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
        public void ClearClips() {
            _clipQueue.Clear();
        }

        public void Draw(DeviceContext dc, Matrix viewProj, EffectPass pass) {
            
            var world = World;
            var wit = MathF.InverseTranspose(world);
            var wvp = world * viewProj;

            Effects.NormalMapFX.SetWorld(world);
            Effects.NormalMapFX.SetWorldInvTranspose(wit);
            Effects.NormalMapFX.SetWorldViewProj(wvp);
            Effects.NormalMapFX.SetTexTransform(Matrix.Identity);

            Effects.NormalMapFX.SetBoneTransforms(FinalTransforms);
            
            for (int i = 0; i < _model.SubsetCount; i++) {
                Effects.NormalMapFX.SetMaterial(_model.Materials[i]);
                Effects.NormalMapFX.SetDiffuseMap(_model.DiffuseMapSRV[i]);
                Effects.NormalMapFX.SetNormalMap(_model.NormalMapSRV[i]);
                
                pass.Apply(dc);
                _model.ModelMesh.Draw(dc, i);
            }
        }
    }
}