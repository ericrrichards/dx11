using SlimDX.Direct3D11;

namespace Core.FX {
    public class NormalMapEffect : BasicEffect {
        
        private readonly EffectResourceVariable _normalMap;
        
        public NormalMapEffect(Device device, string filename) : base(device, filename) {
            _normalMap = FX.GetVariableByName("gNormalMap").AsResource();
        }
        public void SetNormalMap(ShaderResourceView tex) {
            _normalMap.SetResource(tex);
        }
    }
}