using SlimDX;
using SlimDX.Direct3D11;

namespace Core.FX {
    public class WavesEffect : DisplacementMapEffect {

        private readonly EffectMatrixVariable _waveDispTexTransform0;
        private readonly EffectMatrixVariable _waveDispTexTransform1;
        private readonly EffectMatrixVariable _waveNormalTexTransform0;
        private readonly EffectMatrixVariable _waveNormalTexTransform1;
        private readonly EffectScalarVariable _heightScale0;
        private readonly EffectScalarVariable _heightScale1;

        private readonly EffectResourceVariable _normalMap0;
        private readonly EffectResourceVariable _normalMap1;

        public void SetWaveDispTexTransform0(Matrix m) { _waveDispTexTransform0.SetMatrix(m); }
        public void SetWaveDispTexTransform1(Matrix m) { _waveDispTexTransform1.SetMatrix(m); }
        public void SetWaveNormalTexTransform0(Matrix m) { _waveNormalTexTransform0.SetMatrix(m); }
        public void SetWaveNormalTexTransform1(Matrix m) { _waveNormalTexTransform1.SetMatrix(m); }

        public void SetHeightScale0(float f) { _heightScale0.Set(f); }
        public void SetHeightScale1(float f) { _heightScale1.Set(f); }

        public void SetNormalMap0(ShaderResourceView srv) { _normalMap0.SetResource(srv); }
        public void SetNormalMap1(ShaderResourceView srv) { _normalMap1.SetResource(srv); }

        public WavesEffect(Device device, string filename) : base(device, filename) {
            _waveDispTexTransform0 = FX.GetVariableByName("gWaveDispTexTransform0").AsMatrix();
            _waveDispTexTransform1 = FX.GetVariableByName("gWaveDispTexTransform1").AsMatrix();
            _waveNormalTexTransform0 = FX.GetVariableByName("gWaveNormalTexTransform0").AsMatrix();
            _waveNormalTexTransform1 = FX.GetVariableByName("gWaveNormalTexTransform1").AsMatrix();
            _heightScale0 = FX.GetVariableByName("gHeightScale0").AsScalar();
            _heightScale1 = FX.GetVariableByName("gHeightScale1").AsScalar();
            _normalMap0 = FX.GetVariableByName("gNormalMap0").AsResource();
            _normalMap1 = FX.GetVariableByName("gNormalMap1").AsResource();
        }

    }
}