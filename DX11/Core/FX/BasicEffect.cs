namespace Core.FX {
    using System.Collections.Generic;

    using SlimDX;
    using SlimDX.Direct3D11;

    public class BasicEffect : Effect {
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

        public readonly EffectTechnique Light1ReflectTech;
        public readonly EffectTechnique Light2ReflectTech;
        public readonly EffectTechnique Light3ReflectTech;

        public readonly EffectTechnique Light0TexReflectTech;
        public readonly EffectTechnique Light1TexReflectTech;
        public readonly EffectTechnique Light2TexReflectTech;
        public readonly EffectTechnique Light3TexReflectTech;

        public readonly EffectTechnique Light0TexAlphaClipReflectTech;
        public readonly EffectTechnique Light1TexAlphaClipReflectTech;
        public readonly EffectTechnique Light2TexAlphaClipReflectTech;
        public readonly EffectTechnique Light3TexAlphaClipReflectTech;

        public readonly EffectTechnique Light1FogReflectTech;
        public readonly EffectTechnique Light2FogReflectTech;
        public readonly EffectTechnique Light3FogReflectTech;

        public readonly EffectTechnique Light0TexFogReflectTech;
        public readonly EffectTechnique Light1TexFogReflectTech;
        public readonly EffectTechnique Light2TexFogReflectTech;
        public readonly EffectTechnique Light3TexFogReflectTech;

        public readonly EffectTechnique Light0TexAlphaClipFogReflectTech;
        public readonly EffectTechnique Light1TexAlphaClipFogReflectTech;
        public readonly EffectTechnique Light2TexAlphaClipFogReflectTech;
        public readonly EffectTechnique Light3TexAlphaClipFogReflectTech;

        
        private readonly EffectMatrixVariable _worldViewProj;
        private readonly EffectMatrixVariable _world;
        private readonly EffectMatrixVariable _worldInvTranspose;
        private readonly EffectMatrixVariable _texTransform;
        private readonly EffectVectorVariable _eyePosW;
        private readonly EffectVectorVariable _fogColor;
        private readonly EffectScalarVariable _fogStart;
        private readonly EffectScalarVariable _fogRange;
        private readonly EffectVariable _dirLights;
        private readonly EffectVariable _mat;

        private readonly EffectResourceVariable _diffuseMap;
        private readonly EffectResourceVariable _cubeMap;
        

        public BasicEffect(Device device, string filename) : base(device, filename) {
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

            Light1ReflectTech = FX.GetTechniqueByName("Light1Reflect");
            Light2ReflectTech = FX.GetTechniqueByName("Light2Reflect");
            Light3ReflectTech = FX.GetTechniqueByName("Light3Reflect");

            Light0TexReflectTech = FX.GetTechniqueByName("Light0TexReflect");
            Light1TexReflectTech = FX.GetTechniqueByName("Light1TexReflect");
            Light2TexReflectTech = FX.GetTechniqueByName("Light2TexReflect");
            Light3TexReflectTech = FX.GetTechniqueByName("Light3TexReflect");

            Light0TexAlphaClipReflectTech = FX.GetTechniqueByName("Light0TexAlphaClipReflect");
            Light1TexAlphaClipReflectTech = FX.GetTechniqueByName("Light1TexAlphaClipReflect");
            Light2TexAlphaClipReflectTech = FX.GetTechniqueByName("Light2TexAlphaClipReflect");
            Light3TexAlphaClipReflectTech = FX.GetTechniqueByName("Light3TexAlphaClipReflect");

            Light1FogReflectTech = FX.GetTechniqueByName("Light1FogReflect");
            Light2FogReflectTech = FX.GetTechniqueByName("Light2FogReflect");
            Light3FogReflectTech = FX.GetTechniqueByName("Light3FogReflect");

            Light0TexFogReflectTech = FX.GetTechniqueByName("Light0TexFogReflect");
            Light1TexFogReflectTech = FX.GetTechniqueByName("Light1TexFogReflect");
            Light2TexFogReflectTech = FX.GetTechniqueByName("Light2TexFogReflect");
            Light3TexFogReflectTech = FX.GetTechniqueByName("Light3TexFogReflect");

            Light0TexAlphaClipFogReflectTech = FX.GetTechniqueByName("Light0TexAlphaClipFogReflect");
            Light1TexAlphaClipFogReflectTech = FX.GetTechniqueByName("Light1TexAlphaClipFogReflect");
            Light2TexAlphaClipFogReflectTech = FX.GetTechniqueByName("Light2TexAlphaClipFogReflect");
            Light3TexAlphaClipFogReflectTech = FX.GetTechniqueByName("Light3TexAlphaClipFogReflect");

            _worldViewProj = FX.GetVariableByName("gWorldViewProj").AsMatrix();
            _world = FX.GetVariableByName("gWorld").AsMatrix();
            _worldInvTranspose = FX.GetVariableByName("gWorldInvTranspose").AsMatrix();
            _texTransform = FX.GetVariableByName("gTexTransform").AsMatrix();
            _eyePosW = FX.GetVariableByName("gEyePosW").AsVector();

            _fogColor = FX.GetVariableByName("gFogColor").AsVector();
            _fogStart = FX.GetVariableByName("gFogStart").AsScalar();
            _fogRange = FX.GetVariableByName("gFogRange").AsScalar();

            _dirLights = FX.GetVariableByName("gDirLights");
            _mat = FX.GetVariableByName("gMaterial");
            _diffuseMap = FX.GetVariableByName("gDiffuseMap").AsResource();
            _cubeMap = FX.GetVariableByName("gCubeMap").AsResource();

        }
        public void SetWorldViewProj(Matrix m) {
            _worldViewProj.SetMatrix(m);
        }
        public void SetWorld(Matrix m) {
            _world.SetMatrix(m);
        }
        public void SetWorldInvTranspose(Matrix m) {
            _worldInvTranspose.SetMatrix(m);
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

        public void SetTexTransform(Matrix m) {
            _texTransform.SetMatrix(m);
        }

        public void SetDiffuseMap(ShaderResourceView tex) {
            _diffuseMap.SetResource(tex);
        }
        public void SetCubeMap(ShaderResourceView tex) {
            _cubeMap.SetResource(tex);
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
    }
}