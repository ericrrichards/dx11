using System;
using System.Collections.Generic;
using SlimDX;
using SlimDX.Direct3D11;

namespace Core.FX {
    public class InstancedBasicEffect : Effect {
        public readonly EffectTechnique Light1Tech;
        public readonly EffectTechnique Light2Tech;
        public readonly EffectTechnique Light3Tech;

        public readonly EffectTechnique Light0TexTech;
        public readonly EffectTechnique Light1TexTech;
        public readonly EffectTechnique Light2TexTech;
        public readonly EffectTechnique Light3TexTech;

        public readonly EffectTechnique Light0TexAlphaClipTech;
        public readonly EffectTechnique Light1TexAlphaClipTech;
        public readonly EffectTechnique Light2TexAlphaClipTech;
        public readonly EffectTechnique Light3TexAlphaClipTech;

        public readonly EffectTechnique Light1FogTech;
        public readonly EffectTechnique Light2FogTech;
        public readonly EffectTechnique Light3FogTech;

        public readonly EffectTechnique Light0TexFogTech;
        public readonly EffectTechnique Light1TexFogTech;
        public readonly EffectTechnique Light2TexFogTech;
        public readonly EffectTechnique Light3TexFogTech;

        public readonly EffectTechnique Light0TexAlphaClipFogTech;
        public readonly EffectTechnique Light1TexAlphaClipFogTech;
        public readonly EffectTechnique Light2TexAlphaClipFogTech;
        public readonly EffectTechnique Light3TexAlphaClipFogTech;


        private readonly EffectMatrixVariable _viewProj;
        private readonly EffectMatrixVariable _texTransform;
        private readonly EffectVectorVariable _eyePosW;
        private readonly EffectVectorVariable _fogColor;
        private readonly EffectScalarVariable _fogStart;
        private readonly EffectScalarVariable _fogRange;
        private readonly EffectVariable _dirLights;
        public const int MaxLights = 3;
        private readonly byte[] _dirLightsArray = new byte[DirectionalLight.Stride * MaxLights];
        private readonly EffectVariable _mat;

        private readonly EffectResourceVariable _diffuseMap;

        public void SetViewProj(Matrix m) {
            _viewProj.SetMatrix(m);
        }
        
        public void SetEyePosW(Vector3 v) {
            _eyePosW.Set(v);
        }
        public void SetDirLights(DirectionalLight[] lights) {
            System.Diagnostics.Debug.Assert(lights.Length <= MaxLights, "BasicEffect only supports up to 3 lights");

            for (int i = 0; i < lights.Length && i < MaxLights; i++) {
                var light = lights[i];
                var d = Util.GetArray(light);
                Array.Copy(d, 0, _dirLightsArray, i * DirectionalLight.Stride, DirectionalLight.Stride);
            }

            _dirLights.SetRawValue(new DataStream(_dirLightsArray, false, false), _dirLightsArray.Length);
        }
        public void SetMaterial(Material m) {
            var d = Util.GetArray(m);
            _mat.SetRawValue(new DataStream(d, false, false), Material.Stride);
        }

        public void SetTexTransform(Matrix m) {
            _texTransform.SetMatrix(m);
        }

        public void SetDiffuseMap(ShaderResourceView tex) {
            _diffuseMap.SetResource(tex);
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
        public InstancedBasicEffect(Device device, string filename) : base(device, filename) {
            Light1Tech = FX.GetTechniqueByName("Light1");
            Light2Tech = FX.GetTechniqueByName("Light2");
            Light3Tech = FX.GetTechniqueByName("Light3");

            Light0TexTech = FX.GetTechniqueByName("Light0Tex");
            Light1TexTech = FX.GetTechniqueByName("Light1Tex");
            Light2TexTech = FX.GetTechniqueByName("Light2Tex");
            Light3TexTech = FX.GetTechniqueByName("Light3Tex");

            Light0TexAlphaClipTech = FX.GetTechniqueByName("Light0TexAlphaClip");
            Light1TexAlphaClipTech = FX.GetTechniqueByName("Light1TexAlphaClip");
            Light2TexAlphaClipTech = FX.GetTechniqueByName("Light2TexAlphaClip");
            Light3TexAlphaClipTech = FX.GetTechniqueByName("Light3TexAlphaClip");

            Light1FogTech = FX.GetTechniqueByName("Light1Fog");
            Light2FogTech = FX.GetTechniqueByName("Light2Fog");
            Light3FogTech = FX.GetTechniqueByName("Light3Fog");

            Light0TexFogTech = FX.GetTechniqueByName("Light0TexFog");
            Light1TexFogTech = FX.GetTechniqueByName("Light1TexFog");
            Light2TexFogTech = FX.GetTechniqueByName("Light2TexFog");
            Light3TexFogTech = FX.GetTechniqueByName("Light3TexFog");

            Light0TexAlphaClipFogTech = FX.GetTechniqueByName("Light0TexAlphaClipFog");
            Light1TexAlphaClipFogTech = FX.GetTechniqueByName("Light1TexAlphaClipFog");
            Light2TexAlphaClipFogTech = FX.GetTechniqueByName("Light2TexAlphaClipFog");
            Light3TexAlphaClipFogTech = FX.GetTechniqueByName("Light3TexAlphaClipFog");

            _viewProj = FX.GetVariableByName("gViewProj").AsMatrix();
            _texTransform = FX.GetVariableByName("gTexTransform").AsMatrix();
            _eyePosW = FX.GetVariableByName("gEyePosW").AsVector();

            _fogColor = FX.GetVariableByName("gFogColor").AsVector();
            _fogStart = FX.GetVariableByName("gFogStart").AsScalar();
            _fogRange = FX.GetVariableByName("gFogRange").AsScalar();

            _dirLights = FX.GetVariableByName("gDirLights");
            _mat = FX.GetVariableByName("gMaterial");
            _diffuseMap = FX.GetVariableByName("gDiffuseMap").AsResource();
        }
    }
}