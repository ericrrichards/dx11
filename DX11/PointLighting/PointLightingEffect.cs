using SlimDX;
using SlimDX.Direct3D11;
using Effect = Core.FX.Effect;

namespace PointLighting {
    internal class PointLightingEffect : Effect {
        // per object variable
        private readonly EffectMatrixVariable _worldViewProj;
        private readonly EffectMatrixVariable _world;
        private readonly EffectMatrixVariable _worldInvTranspose;

        private readonly EffectVectorVariable _eyePosition;
        private readonly EffectScalarVariable _specularExponent;
        private readonly EffectScalarVariable _specularIntensity;

        private readonly EffectVectorVariable _pointLightPosition;
        private readonly EffectScalarVariable _pointLightRangeRcp;
        private readonly EffectVectorVariable _pointLightColor;

        private readonly EffectResourceVariable _diffuseMap;
        public readonly EffectTechnique DepthPrePass;
        public readonly EffectTechnique PointLight;

        public PointLightingEffect(Device device, string filename) : base(device, filename) {

            DepthPrePass = FX.GetTechniqueByName("DepthPrePass");
            PointLight = FX.GetTechniqueByName("Point");

            _worldViewProj = FX.GetVariableByName("WorldViewProjection").AsMatrix();
            _world = FX.GetVariableByName("World").AsMatrix();
            _worldInvTranspose = FX.GetVariableByName("gWorldInvTranspose").AsMatrix();

            _eyePosition = FX.GetVariableByName("EyePosition").AsVector();
            _specularExponent = FX.GetVariableByName("specExp").AsScalar();
            _specularIntensity = FX.GetVariableByName("specIntensity").AsScalar();

            _pointLightPosition = FX.GetVariableByName("PointLightPosition").AsVector();
            _pointLightRangeRcp = FX.GetVariableByName("PointLightRangeRcp").AsScalar();
            _pointLightColor = FX.GetVariableByName("PointLightColor").AsVector();

            _diffuseMap = FX.GetVariableByName("DiffuseTexture").AsResource();
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
        public void SetEyePosition(Vector3 p) {
            _eyePosition.Set(p);
        }

        public void SetSpecularExponent(float e) {
            _specularExponent.Set(e);
        }

        public void SetSpecularIntensity(float i) {
            _specularIntensity.Set(i);
        }
        public void SetDiffuseMap(ShaderResourceView tex) {
            _diffuseMap.SetResource(tex);
        }

        public void SetLightPosition(Vector3 p) {
            _pointLightPosition.Set(p);
        }

        public void SetLightColor(Vector3 c) {
            _pointLightColor.Set(c);
        }

        public void SetLightRangeRcp(float f) {
            _pointLightRangeRcp.Set(f);
        }
    }
}