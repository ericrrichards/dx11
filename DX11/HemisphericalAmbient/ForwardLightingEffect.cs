using SlimDX;
using SlimDX.Direct3D11;
using Effect = Core.FX.Effect;

namespace HemisphericalAmbient {
    public class ForwardLightingEffect : Effect {
        private readonly EffectMatrixVariable _worldViewProj;
        private readonly EffectMatrixVariable _world;

        private readonly EffectVectorVariable _ambientDown;
        private readonly EffectVectorVariable _ambientRange;

        private readonly EffectResourceVariable _diffuseMap;

        public readonly EffectTechnique Ambient;

        public ForwardLightingEffect(Device device, string filename) : base(device, filename) {

            Ambient = FX.GetTechniqueByName("Ambient");

            _worldViewProj = FX.GetVariableByName("WorldViewProjection").AsMatrix();
            _world = FX.GetVariableByName("World").AsMatrix();

            _ambientDown = FX.GetVariableByName("AmbientDown").AsVector();
            _ambientRange = FX.GetVariableByName("AmbientRange").AsVector();

            _diffuseMap = FX.GetVariableByName("DiffuseTexture").AsResource();
        }

        public void SetWorldViewProj(Matrix m) {
            _worldViewProj.SetMatrix(m);
        }

        public void SetWorld(Matrix m) {
            _world.SetMatrix(m);
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
    }
}