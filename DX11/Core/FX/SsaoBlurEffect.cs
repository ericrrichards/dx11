using SlimDX.Direct3D11;

namespace Core.FX {
    public class SsaoBlurEffect : Effect {
        public EffectTechnique HorzBlurTech { get; private set; }
        public EffectTechnique VertBlurTech { get; private set; }

        private readonly EffectScalarVariable _texelWidth;
        private readonly EffectScalarVariable _texelHeight;

        private readonly EffectResourceVariable _normalDepthMap;
        private readonly EffectResourceVariable _inputImage;

        public void SetTexelWidth(float f) {_texelWidth.Set(f);}
        public void SetTexelHeight(float f) {_texelHeight.Set(f);}
        public void SetNormalDepthMap(ShaderResourceView srv) {_normalDepthMap.SetResource(srv);}
        public void SetInputImage(ShaderResourceView srv) {_inputImage.SetResource(srv);}

        public SsaoBlurEffect(Device device, string filename) : base(device, filename) {
            HorzBlurTech = FX.GetTechniqueByName("HorzBlur");
            VertBlurTech = FX.GetTechniqueByName("VertBlur");

            _texelWidth = FX.GetVariableByName("gTexelWidth").AsScalar();
            _texelHeight = FX.GetVariableByName("gTexelHeight").AsScalar();

            _normalDepthMap = FX.GetVariableByName("gNormalDepthMap").AsResource();
            _inputImage = FX.GetVariableByName("gInputImage").AsResource();
        }
    }
}