using SlimDX;
using SlimDX.Direct3D11;

namespace Core.FX {
    public class SsaoNormalDepthEffect : Effect {
        public EffectTechnique NormalDepthTech { get; private set; }
        public EffectTechnique NormalDepthAlphaClipTech { get; private set; }

        private readonly EffectMatrixVariable _worldView;
        private readonly EffectMatrixVariable _worldInvTransposeView;
        private readonly EffectMatrixVariable _worldViewProj;
        private readonly EffectMatrixVariable _texTransform;

        private readonly EffectResourceVariable _diffuseMap;

        public void SetWorldView(Matrix m) { _worldView.SetMatrix(m); }
        public void SetWorldInvTransposeView(Matrix m) { _worldInvTransposeView.SetMatrix(m); }
        public void SetWorldViewProj(Matrix m) { _worldViewProj.SetMatrix(m); }
        public void SetTexTransform(Matrix m) { _texTransform.SetMatrix(m); }
        public void SetDiffuseMap(ShaderResourceView srv) { _diffuseMap.SetResource(srv); }

        public SsaoNormalDepthEffect(Device device, string filename) : base(device, filename) {
            NormalDepthTech = FX.GetTechniqueByName("NormalDepth");
            NormalDepthAlphaClipTech = FX.GetTechniqueByName("NormalDepthAlphaClip");

            _worldView = FX.GetVariableByName("gWorldView").AsMatrix();
            _worldInvTransposeView = FX.GetVariableByName("gWorldInvTransposeView").AsMatrix();
            _worldViewProj = FX.GetVariableByName("gWorldViewProj").AsMatrix();
            _texTransform = FX.GetVariableByName("gTexTransform").AsMatrix();
            _diffuseMap = FX.GetVariableByName("gDiffuseMap").AsResource();
        }
    }
}