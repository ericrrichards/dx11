using SlimDX;
using SlimDX.Direct3D11;

namespace Core.FX {
    public class SsaoEffect : Effect {
        public EffectTechnique SsaoTech { get; private set; }
        private readonly EffectMatrixVariable _viewToTexSpace;
        private readonly EffectVectorVariable _frustumCorners;
        private readonly EffectVectorVariable _offsetVectors;
        private readonly EffectScalarVariable _occlusionRadius;
        private readonly EffectScalarVariable _occlusionFadeStart;
        private readonly EffectScalarVariable _occlusionFadeEnd;
        private readonly EffectScalarVariable _surfaceEpsilon;

        private readonly EffectResourceVariable _normalDepthMap;
        private readonly EffectResourceVariable _randomVecMap;

        public void SetViewToTexSpace(Matrix m) {_viewToTexSpace.SetMatrix(m);}
        public void SetOffsetVectors(Vector4[] v) { _offsetVectors.Set(v); }
        public void SetFrustumCorners(Vector4[] v) { _frustumCorners.Set(v); }
        public void SetOcclusionRadius(float f) { _occlusionRadius.Set(f); }
        public void SetOcclusionFadeStart(float f) { _occlusionFadeStart.Set(f); }
        public void SetOcclusionFadeEnd(float f) { _occlusionFadeEnd.Set(f); }
        public void SetSurfaceEpsilon(float f) { _surfaceEpsilon.Set(f); }

        public void SetNormalDepthMap(ShaderResourceView srv) { _normalDepthMap.SetResource(srv); }
        public void SetRandomVecMap(ShaderResourceView srv) { _randomVecMap.SetResource(srv); }

        public SsaoEffect(Device device, string filename) : base(device, filename) {
            SsaoTech = FX.GetTechniqueByName("Ssao");

            _viewToTexSpace = FX.GetVariableByName("gViewToTexSpace").AsMatrix();
            _offsetVectors = FX.GetVariableByName("gOffsetVectors").AsVector();
            _frustumCorners = FX.GetVariableByName("gFrustumCorners").AsVector();
            _occlusionRadius = FX.GetVariableByName("gOcclusionRadius").AsScalar();
            _occlusionFadeStart = FX.GetVariableByName("gOcclusionFadeStart").AsScalar();
            _occlusionFadeEnd = FX.GetVariableByName("gOcclusionFadeEnd").AsScalar();
            _surfaceEpsilon = FX.GetVariableByName("gSurfaceEpsilon").AsScalar();

            _normalDepthMap = FX.GetVariableByName("gNormalDepthMap").AsResource();
            _randomVecMap = FX.GetVariableByName("gRandomVecMap").AsResource();
        }
    }
}