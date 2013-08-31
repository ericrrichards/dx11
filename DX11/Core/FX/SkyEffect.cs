namespace Core.FX {
    using SlimDX;
    using SlimDX.Direct3D11;

    public class SkyEffect : Effect {
        public SkyEffect(Device device, string filename)
            : base(device, filename) {
            SkyTech = FX.GetTechniqueByName("SkyTech");
            WorldViewProj = FX.GetVariableByName("gWorldViewProj").AsMatrix();
            CubeMap = FX.GetVariableByName("gCubeMap").AsResource();
        }

        private EffectMatrixVariable WorldViewProj { get; set; }
        private EffectResourceVariable CubeMap { get; set; }
        public EffectTechnique SkyTech { get; private set; }

        public void SetWorldViewProj(Matrix m) {
            WorldViewProj.SetMatrix(m);
        }
        public void SetCubeMap(ShaderResourceView cubemap) {
            CubeMap.SetResource(cubemap);
        }

    }
}