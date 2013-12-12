namespace Core.FX {
    using System;
    using System.Collections.Generic;

    using SlimDX;
    using SlimDX.Direct3D11;

    public class InstancedNormalMapEffect : Effect {
        public readonly EffectTechnique Light3TexTech;
        public readonly EffectTechnique Light1Tech;

        private readonly EffectMatrixVariable _viewProj;
        private readonly EffectMatrixVariable _texTransform;
        private readonly EffectVectorVariable _eyePosW;

        private readonly EffectVariable _dirLights;
        public const int MaxLights = 3;
        private readonly byte[] _dirLightsArray = new byte[DirectionalLight.Stride * MaxLights];

        private readonly EffectVariable _mat;

        private readonly EffectResourceVariable _normalMap;
        private readonly EffectResourceVariable _diffuseMap;

        public InstancedNormalMapEffect(Device device, string filename) : base(device, filename) {
            Light1Tech = FX.GetTechniqueByName("Light1");
            Light3TexTech = FX.GetTechniqueByName("Light3Tex");

            _viewProj = FX.GetVariableByName("gViewProj").AsMatrix();
            _texTransform = FX.GetVariableByName("gTexTransform").AsMatrix();
            _eyePosW = FX.GetVariableByName("gEyePosW").AsVector();

            _dirLights = FX.GetVariableByName("gDirLights");
            _mat = FX.GetVariableByName("gMaterial");
            _diffuseMap = FX.GetVariableByName("gDiffuseMap").AsResource();
            _normalMap = FX.GetVariableByName("gNormalMap").AsResource();
        }

        public void SetViewProj(Matrix m) {
            _viewProj.SetMatrix(m);
        }

        public void SetTexTransform(Matrix m) {
            _texTransform.SetMatrix(m);
        }

        public void SetMaterial(Material mat) {
            var d = Util.GetArray(mat);
            _mat.SetRawValue(new DataStream(d, false, false), Material.Stride);
        }

        public void SetDiffuseMap(ShaderResourceView tex) {
            _diffuseMap.SetResource(tex);
        }

        public void SetNormalMap(ShaderResourceView tex) {
            _normalMap.SetResource(tex);
        }

        public void SetDirLights(DirectionalLight[] lights) {
            System.Diagnostics.Debug.Assert(lights.Length <= MaxLights, "BasicEffect only supports up to 3 lights");

            for (int i = 0; i < lights.Length && i < MaxLights; i++) {
                var light = lights[i];
                var d = Util.GetArray(light);
                Array.Copy(d, 0, _dirLightsArray, i * DirectionalLight.Stride, DirectionalLight.Stride);
            }

            _dirLights.SetRawValue(new DataStream(_dirLightsArray, false, false), _dirLightsArray.Length);
        }

        public void SetEyePosW(Vector3 v) {
            _eyePosW.Set(v);
        }
    }
}