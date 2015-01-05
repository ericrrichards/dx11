using SlimDX;
using SlimDX.Direct3D11;
using Effect = Core.FX.Effect;

namespace PointLighting {
    internal class DirectionalLightingEffect : Effect {
        // per object variable
        private readonly EffectMatrixVariable _worldViewProj;
        private readonly EffectMatrixVariable _world;
        private readonly EffectMatrixVariable _worldInvTranspose;
        // scene variables
        private readonly EffectVectorVariable _ambientDown;
        private readonly EffectVectorVariable _ambientRange;
        private readonly EffectVectorVariable _dirToLight;
        private readonly EffectVectorVariable _dirLightColor;
        
        private readonly EffectVectorVariable _eyePosition;
        private readonly EffectScalarVariable _specularExponent;
        private readonly EffectScalarVariable _specularIntensity;
        // per subset diffuse texture
        private readonly EffectResourceVariable _diffuseMap;
        // ambient effect technique
        public readonly EffectTechnique Ambient;
        public readonly EffectTechnique DepthPrePass;
        public readonly EffectTechnique Directional;

        public DirectionalLightingEffect(Device device, string filename)
            : base(device, filename) {

            Ambient = FX.GetTechniqueByName("Ambient");
            DepthPrePass = FX.GetTechniqueByName("DepthPrePass");
            Directional = FX.GetTechniqueByName("Directional");


            _worldViewProj = FX.GetVariableByName("WorldViewProjection").AsMatrix();
            _world = FX.GetVariableByName("World").AsMatrix();
            _worldInvTranspose = FX.GetVariableByName("gWorldInvTranspose").AsMatrix();

            _ambientDown = FX.GetVariableByName("AmbientDown").AsVector();
            _ambientRange = FX.GetVariableByName("AmbientRange").AsVector();

            _dirToLight = FX.GetVariableByName("DirToLight").AsVector();
            _dirLightColor = FX.GetVariableByName("DirLightColor").AsVector();

            _eyePosition = FX.GetVariableByName("EyePosition").AsVector();
            _specularExponent = FX.GetVariableByName("specExp").AsScalar();
            _specularIntensity = FX.GetVariableByName("specIntensity").AsScalar();

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

        public void SetAmbientDown(Vector3 v) {
            _ambientDown.Set(v);
        }

        public void SetAmbientRange(Vector3 v) {
            _ambientRange.Set(v);
        }
        public void SetDiffuseMap(ShaderResourceView tex) {
            _diffuseMap.SetResource(tex);
        }

        public void SetDirectLightDirection(Vector3 v) {
            _dirToLight.Set(v);
        }

        public void SetDirectLightColor(Vector3 c) {
            _dirLightColor.Set(c);
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
    }
}