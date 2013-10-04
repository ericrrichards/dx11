namespace Core.FX {
    using SlimDX;
    using SlimDX.Direct3D11;

    public class DebugTexEffect : Effect {
        public readonly EffectTechnique ViewArgbTech;
        public readonly EffectTechnique ViewRedTech;
        public readonly EffectTechnique ViewGreenTech;
        public readonly EffectTechnique ViewBlueTech;
        public readonly EffectTechnique ViewAlphaTech;

        private readonly EffectMatrixVariable _wvp;
        private readonly EffectResourceVariable _texture;

        public void SetWorldViewProj(Matrix m) {
            _wvp.SetMatrix(m);
        }
        public void SetTexture(ShaderResourceView tex) {
            _texture.SetResource(tex);
        }


        public DebugTexEffect(Device device, string filename) : base(device, filename) {
            ViewArgbTech = FX.GetTechniqueByName("ViewArgbTech");
            ViewRedTech = FX.GetTechniqueByName("ViewRedTech");
            ViewGreenTech = FX.GetTechniqueByName("ViewGreenTech");
            ViewBlueTech = FX.GetTechniqueByName("ViewBlueTech");
            ViewAlphaTech = FX.GetTechniqueByName("ViewAlphaTech");

            _texture = FX.GetVariableByName("gTexture").AsResource();
            _wvp = FX.GetVariableByName("gWorldViewProj").AsMatrix();
        }
    }
}