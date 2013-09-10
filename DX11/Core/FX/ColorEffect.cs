using SlimDX;
using SlimDX.Direct3D11;

namespace Core.FX {
    public class ColorEffect :Effect {
        public readonly EffectTechnique ColorTech;
        private readonly EffectMatrixVariable _wvp;

        public ColorEffect(Device device, string filename) : base(device, filename) {
            ColorTech = FX.GetTechniqueByName("ColorTech");
            _wvp = FX.GetVariableByName("gWorldViewProj").AsMatrix();
        }
        public void SetWorldViewProj(Matrix m) {
            _wvp.SetMatrix(m);
        }
    }
}