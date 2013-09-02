namespace Core.FX {
    using System;
    using System.Text;

    using SlimDX;
    using SlimDX.Direct3D11;

    public static class Effects {
        public static void InitAll(Device device) {
            try {
                BasicFX = new BasicEffect(device, "FX/Basic.fxo");
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
            try {
                TreeSpriteFX = new TreeSpriteEffect(device, "FX/TreeSprite.fxo");
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
            try {
                InstancedBasicFX = new InstancedBasicEffect(device, "FX/InstancedBasic.fxo");
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
            try {
                SkyFX = new SkyEffect(device, "FX/Sky.fxo");
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
            try {
                NormalMapFX = new NormalMapEffect(device, "FX/NormalMap.fxo");
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
            try {
                DisplacementMapFX = new DisplacementMapEffect(device, "FX/DisplacementMap.fxo");
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }
        public static void DestroyAll() {
            Util.ReleaseCom(ref BasicFX);
            Util.ReleaseCom(ref TreeSpriteFX);
            Util.ReleaseCom(ref InstancedBasicFX);
            Util.ReleaseCom(ref SkyFX);
            Util.ReleaseCom(ref NormalMapFX);
            Util.ReleaseCom(ref DisplacementMapFX);

        }

        public static BasicEffect BasicFX;
        public static TreeSpriteEffect TreeSpriteFX;
        public static InstancedBasicEffect InstancedBasicFX;
        public static SkyEffect SkyFX;
        public static NormalMapEffect NormalMapFX;
        public static DisplacementMapEffect DisplacementMapFX;
    }

    public class NormalMapEffect : BasicEffect {
        private readonly EffectResourceVariable _normalMap;
        public NormalMapEffect(Device device, string filename) : base(device, filename) {
            _normalMap = FX.GetVariableByName("gNormalMap").AsResource();
        }
        public void SetNormalMap(ShaderResourceView tex) {
            _normalMap.SetResource(tex);
        }
    }
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
