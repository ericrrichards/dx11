using SlimDX;
using SlimDX.Direct3D11;

namespace Core.FX {
    public class ParticleEffect : Effect {
        public readonly EffectTechnique StreamOutTech;
        public readonly EffectTechnique DrawTech;

        private readonly EffectMatrixVariable _viewProj;
        private readonly EffectScalarVariable _timeStep;
        private readonly EffectScalarVariable _gameTime;
        private readonly EffectVectorVariable _eyePosW;
        private readonly EffectVectorVariable _emitPosW;
        private readonly EffectVectorVariable _emitDirW;
        private readonly EffectResourceVariable _texArray;
        private readonly EffectResourceVariable _randomTex;


        public ParticleEffect(Device device, string filename) : base(device, filename) {
            StreamOutTech = FX.GetTechniqueByName("StreamOutTech");
            DrawTech = FX.GetTechniqueByName("DrawTech");

            _viewProj = FX.GetVariableByName("gViewProj").AsMatrix();
            _gameTime = FX.GetVariableByName("gGameTime").AsScalar();
            _timeStep = FX.GetVariableByName("gTimeStep").AsScalar();

            _eyePosW = FX.GetVariableByName("gEyePosW").AsVector();
            _emitPosW = FX.GetVariableByName("gEmitPosW").AsVector();
            _emitDirW = FX.GetVariableByName("gEmitDirW").AsVector();

            _texArray = FX.GetVariableByName("gTexArray").AsResource();
            _randomTex = FX.GetVariableByName("gRandomTex").AsResource();
        }

        public void SetViewProj(Matrix m) {
            _viewProj.SetMatrix(m);
        }
        public void SetGameTime(float f) {
            _gameTime.Set(f);
        }
        public void SetTimeStep(float f) {
            _timeStep.Set(f);
        }
        public void SetEyePosW(Vector3 v) {
            _eyePosW.Set(v);
        }
        public void SetEmitPosW(Vector3 v) {
            _emitPosW.Set(v);
        }
        public void SetEmitDirW(Vector3 v) {
            _emitDirW.Set(v);
        }
        public void SetTexArray(ShaderResourceView tex) {
            _texArray.SetResource(tex);
        }
        public void SetRandomTex(ShaderResourceView tex) {
            _randomTex.SetResource(tex);
        }

    }
}