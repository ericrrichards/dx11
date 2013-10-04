namespace Core.FX {
    using SlimDX;
    using SlimDX.Direct3D11;

    public class BuildShadowMapEffect : Effect {
        public readonly EffectTechnique BuildShadowMapTech;
        public readonly EffectTechnique BuildShadowMapAlphaClipTech;
        public readonly EffectTechnique TessBuildShadowMapTech;
        public readonly EffectTechnique TessBuildShadowMapAlphaClipTech;

        private readonly EffectMatrixVariable _viewProj;
        private readonly EffectMatrixVariable _worldViewProj;
        private readonly EffectMatrixVariable _world;
        private readonly EffectMatrixVariable _worldInvTranspose;
        private readonly EffectMatrixVariable _texTransform;
        private readonly EffectVectorVariable _eyePosW;
        private readonly EffectScalarVariable _heightScale;
        private readonly EffectScalarVariable _maxTessDistance;
        private readonly EffectScalarVariable _minTessDistance;
        private readonly EffectScalarVariable _minTessFactor;
        private readonly EffectScalarVariable _maxTessFactor;

        private readonly EffectResourceVariable _diffuseMap;
        private readonly EffectResourceVariable _normalMap;


        public BuildShadowMapEffect(Device device, string filename) : base(device, filename) {
            BuildShadowMapTech = FX.GetTechniqueByName("BuildShadowMapTech");
            BuildShadowMapAlphaClipTech = FX.GetTechniqueByName("BuildShadowMapAlphaClipTech");
            TessBuildShadowMapTech = FX.GetTechniqueByName("TessBuildShadowMapTech"); 
            TessBuildShadowMapAlphaClipTech = FX.GetTechniqueByName("TessBuildShadowMapAlphaClipTech");

            _heightScale = FX.GetVariableByName("gHeightScale").AsScalar();
            _maxTessDistance = FX.GetVariableByName("gMaxTessDistance").AsScalar();
            _minTessDistance = FX.GetVariableByName("gMinTessDistance").AsScalar();
            _minTessFactor = FX.GetVariableByName("gMinTessFactor").AsScalar();
            _maxTessFactor = FX.GetVariableByName("gMaxTessFactor").AsScalar();
            _viewProj = FX.GetVariableByName("gViewProj").AsMatrix();

            _normalMap = FX.GetVariableByName("gNormalMap").AsResource();
            _diffuseMap = FX.GetVariableByName("gDiffuseMap").AsResource();

            _worldViewProj = FX.GetVariableByName("gWorldViewProj").AsMatrix();
            _world = FX.GetVariableByName("gWorld").AsMatrix();
            _worldInvTranspose = FX.GetVariableByName("gWorldInvTranspose").AsMatrix();
            _texTransform = FX.GetVariableByName("gTexTransform").AsMatrix();
            _eyePosW = FX.GetVariableByName("gEyePosW").AsVector();
        }
        public void SetHeightScale(float f) {
            _heightScale.Set(f);
        }

        public void SetMaxTessDistance(float f) {
            _maxTessDistance.Set(f);
        }

        public void SetMinTessDistance(float f) {
            _minTessDistance.Set(f);
        }

        public void SetMinTessFactor(float f) {
            _minTessFactor.Set(f);
        }

        public void SetMaxTessFactor(float f) {
            _maxTessFactor.Set(f);
        }

        public void SetViewProj(Matrix viewProj) {
            _viewProj.SetMatrix(viewProj);
        }
        public void SetNormalMap(ShaderResourceView tex) {
            _normalMap.SetResource(tex);
        }
        public void SetTexTransform(Matrix m) {
            _texTransform.SetMatrix(m);
        }

        public void SetDiffuseMap(ShaderResourceView tex) {
            _diffuseMap.SetResource(tex);
        }
        public void SetWorldViewProj(Matrix m) {
            _worldViewProj.SetMatrix(m);
        }
        public void SetWorld(Matrix m) {
            _world.SetMatrix(m);
        }
        public void SetWorldInvTranspose(Matrix m) {
            _worldInvTranspose.SetMatrix(m);
        }
        public void SetEyePosW(Vector3 v) {
            _eyePosW.Set(v);
        }
    }
}