using SlimDX;
using SlimDX.Direct3D11;

namespace Core.FX {
    public class DisplacementMapEffect : NormalMapEffect {
        private readonly EffectScalarVariable _heightScale;
        private readonly EffectScalarVariable _maxTessDistance;
        private readonly EffectScalarVariable _minTessDistance;
        private readonly EffectScalarVariable _minTessFactor;
        private readonly EffectScalarVariable _maxTessFactor;
        private readonly EffectMatrixVariable _viewProj;

        public DisplacementMapEffect(Device device, string filename) : base(device, filename) {
            _heightScale = FX.GetVariableByName("gHeightScale").AsScalar();
            _maxTessDistance = FX.GetVariableByName("gMaxTessDistance").AsScalar();
            _minTessDistance = FX.GetVariableByName("gMinTessDistance").AsScalar();
            _minTessFactor = FX.GetVariableByName("gMinTessFactor").AsScalar();
            _maxTessFactor = FX.GetVariableByName("gMaxTessFactor").AsScalar();
            _viewProj = FX.GetVariableByName("gViewProj").AsMatrix();
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
    }
}