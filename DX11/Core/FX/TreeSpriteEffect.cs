using System.Collections.Generic;

namespace Core.FX {
    using SlimDX;
    using SlimDX.Direct3D11;

    public class TreeSpriteEffect : Effect {
        public readonly EffectTechnique Light3Tech;
        public readonly EffectTechnique Light3TexAlphaClipTech;
        public readonly EffectTechnique Light3TexAlphaClipFogTech;

        private readonly EffectMatrixVariable _viewProj;
        private readonly EffectVectorVariable _eyePosW;
        private readonly EffectVectorVariable _fogColor;
        private readonly EffectScalarVariable _fogStart;
        private readonly EffectScalarVariable _fogRange;
        private readonly EffectVariable _dirLights;
        private readonly EffectVariable _mat;
        private readonly EffectResourceVariable _treeTextureMapArray;

        public TreeSpriteEffect(Device device, string filename) : base(device, filename) {
            Light3Tech = FX.GetTechniqueByName("Light3");
            Light3TexAlphaClipTech = FX.GetTechniqueByName("Light3TexAlphaClip");
            Light3TexAlphaClipFogTech = FX.GetTechniqueByName("Light3TexAlphaClipFog");

            _viewProj = FX.GetVariableByName("gViewProj").AsMatrix();
            _eyePosW = FX.GetVariableByName("gEyePosW").AsVector();
            _fogColor = FX.GetVariableByName("gFogColor").AsVector();
            _fogStart = FX.GetVariableByName("gFogStart").AsScalar();
            _fogRange = FX.GetVariableByName("gFogRange").AsScalar();
            _dirLights = FX.GetVariableByName("gDirLights");
            _mat = FX.GetVariableByName("gMaterial");
            _treeTextureMapArray = FX.GetVariableByName("gTreeMapArray").AsResource();
        }

        public void SetViewProj( Matrix m) {
            _viewProj.SetMatrix(m);
        }
        public void SetEyePosW(Vector3 v) {
            _eyePosW.Set(v);
        }
        public void SetDirLights(DirectionalLight[] lights) {
            System.Diagnostics.Debug.Assert(lights.Length <= 3, "BasicEffect only supports up to 3 lights");
            var array = new List<byte>();
            foreach (var light in lights) {
                var d = Util.GetArray(light);
                array.AddRange(d);
            }

            _dirLights.SetRawValue(new DataStream(array.ToArray(), false, false), array.Count);
        }
        public void SetMaterial(Material m) {
            var d = Util.GetArray(m);
            _mat.SetRawValue(new DataStream(d, false, false), d.Length);
        }
        public void SetFogColor(Color4 c) {
            _fogColor.Set(c);
        }
        public void SetFogStart(float f) {
            _fogStart.Set(f);
        }
        public void SetFogRange(float f) {
            _fogRange.Set(f);
        }
        public void SetTreeTextrueMapArray( ShaderResourceView tex) {
            _treeTextureMapArray.SetResource(tex);
        }
    }
}
