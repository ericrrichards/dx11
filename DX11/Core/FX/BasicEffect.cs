using System;

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

        public readonly EffectTechnique Light1SkinnedTech;
        public readonly EffectTechnique Light2SkinnedTech;
        public readonly EffectTechnique Light3SkinnedTech;

        public readonly EffectTechnique Light0TexSkinnedTech;
        public readonly EffectTechnique Light1TexSkinnedTech;
        public readonly EffectTechnique Light2TexSkinnedTech;
        public readonly EffectTechnique Light3TexSkinnedTech;

        public readonly EffectTechnique Light0TexAlphaClipSkinnedTech;
        public readonly EffectTechnique Light1TexAlphaClipSkinnedTech;
        public readonly EffectTechnique Light2TexAlphaClipSkinnedTech;
        public readonly EffectTechnique Light3TexAlphaClipSkinnedTech;

        public readonly EffectTechnique Light1FogSkinnedTech;
        public readonly EffectTechnique Light2FogSkinnedTech;
        public readonly EffectTechnique Light3FogSkinnedTech;

        public readonly EffectTechnique Light0TexFogSkinnedTech;
        public readonly EffectTechnique Light1TexFogSkinnedTech;
        public readonly EffectTechnique Light2TexFogSkinnedTech;
        public readonly EffectTechnique Light3TexFogSkinnedTech;

        public readonly EffectTechnique Light0TexAlphaClipFogSkinnedTech;
        public readonly EffectTechnique Light1TexAlphaClipFogSkinnedTech;
        public readonly EffectTechnique Light2TexAlphaClipFogSkinnedTech;
        public readonly EffectTechnique Light3TexAlphaClipFogSkinnedTech;

        public readonly EffectTechnique Light1ReflectSkinnedTech;
        public readonly EffectTechnique Light2ReflectSkinnedTech;
        public readonly EffectTechnique Light3ReflectSkinnedTech;

        public readonly EffectTechnique Light0TexReflectSkinnedTech;
        public readonly EffectTechnique Light1TexReflectSkinnedTech;
        public readonly EffectTechnique Light2TexReflectSkinnedTech;
        public readonly EffectTechnique Light3TexReflectSkinnedTech;

        public readonly EffectTechnique Light0TexAlphaClipReflectSkinnedTech;
        public readonly EffectTechnique Light1TexAlphaClipReflectSkinnedTech;
        public readonly EffectTechnique Light2TexAlphaClipReflectSkinnedTech;
        public readonly EffectTechnique Light3TexAlphaClipReflectSkinnedTech;

        public readonly EffectTechnique Light1FogReflectSkinnedTech;
        public readonly EffectTechnique Light2FogReflectSkinnedTech;
        public readonly EffectTechnique Light3FogReflectSkinnedTech;

        public readonly EffectTechnique Light0TexFogReflectSkinnedTech;
        public readonly EffectTechnique Light1TexFogReflectSkinnedTech;
        public readonly EffectTechnique Light2TexFogReflectSkinnedTech;
        public readonly EffectTechnique Light3TexFogReflectSkinnedTech;

        public readonly EffectTechnique Light0TexAlphaClipFogReflectSkinnedTech;
        public readonly EffectTechnique Light1TexAlphaClipFogReflectSkinnedTech;
        public readonly EffectTechnique Light2TexAlphaClipFogReflectSkinnedTech;
        public readonly EffectTechnique Light3TexAlphaClipFogReflectSkinnedTech;

        private readonly EffectMatrixVariable _worldViewProj;
        private readonly EffectMatrixVariable _world;
        private readonly EffectMatrixVariable _worldInvTranspose;
        private readonly EffectMatrixVariable _texTransform;
        private readonly EffectMatrixVariable _shadowTransform;
        private readonly EffectVectorVariable _eyePosW;
        private readonly EffectVectorVariable _fogColor;
        private readonly EffectScalarVariable _fogStart;
        private readonly EffectScalarVariable _fogRange;

        private readonly EffectVariable _dirLights;
        public const int MaxLights = 3;
        private readonly byte[] _dirLightsArray = new byte[DirectionalLight.Stride*MaxLights];

        private readonly EffectMatrixVariable _boneTransforms;
        public const int MaxBones = 96;
        private readonly Matrix[] _boneTransformsArray = new Matrix[MaxBones];


        private readonly EffectVariable _mat;

        private readonly EffectResourceVariable _diffuseMap;
        private readonly EffectResourceVariable _shadowMap;
        private readonly EffectResourceVariable _cubeMap;
        
        private readonly EffectResourceVariable _ssaoMap;
        private readonly EffectMatrixVariable _worldViewProjTex;


        public BasicEffect(Device device, string filename)
            : base(device, filename) {
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

            // skinned techs
            Light1SkinnedTech = FX.GetTechniqueByName("Light1Skinned");
            Light2SkinnedTech = FX.GetTechniqueByName("Light2Skinned");
            Light3SkinnedTech = FX.GetTechniqueByName("Light3Skinned");

            Light0TexSkinnedTech = FX.GetTechniqueByName("Light0TexSkinned");
            Light1TexSkinnedTech = FX.GetTechniqueByName("Light1TexSkinned");
            Light2TexSkinnedTech = FX.GetTechniqueByName("Light2TexSkinned");
            Light3TexSkinnedTech = FX.GetTechniqueByName("Light3TexSkinned");

            Light0TexAlphaClipSkinnedTech = FX.GetTechniqueByName("Light0TexAlphaClipSkinned");
            Light1TexAlphaClipSkinnedTech = FX.GetTechniqueByName("Light1TexAlphaClipSkinned");
            Light2TexAlphaClipSkinnedTech = FX.GetTechniqueByName("Light2TexAlphaClipSkinned");
            Light3TexAlphaClipSkinnedTech = FX.GetTechniqueByName("Light3TexAlphaClipSkinned");

            Light1FogSkinnedTech = FX.GetTechniqueByName("Light1FogSkinned");
            Light2FogSkinnedTech = FX.GetTechniqueByName("Light2FogSkinned");
            Light3FogSkinnedTech = FX.GetTechniqueByName("Light3FogSkinned");

            Light0TexFogSkinnedTech = FX.GetTechniqueByName("Light0TexFogSkinned");
            Light1TexFogSkinnedTech = FX.GetTechniqueByName("Light1TexFogSkinned");
            Light2TexFogSkinnedTech = FX.GetTechniqueByName("Light2TexFogSkinned");
            Light3TexFogSkinnedTech = FX.GetTechniqueByName("Light3TexFogSkinned");

            Light0TexAlphaClipFogSkinnedTech = FX.GetTechniqueByName("Light0TexAlphaClipFogSkinned");
            Light1TexAlphaClipFogSkinnedTech = FX.GetTechniqueByName("Light1TexAlphaClipFogSkinned");
            Light2TexAlphaClipFogSkinnedTech = FX.GetTechniqueByName("Light2TexAlphaClipFogSkinned");
            Light3TexAlphaClipFogSkinnedTech = FX.GetTechniqueByName("Light3TexAlphaClipFogSkinned");

            Light1ReflectSkinnedTech = FX.GetTechniqueByName("Light1ReflectSkinned");
            Light2ReflectSkinnedTech = FX.GetTechniqueByName("Light2ReflectSkinned");
            Light3ReflectSkinnedTech = FX.GetTechniqueByName("Light3ReflectSkinned");

            Light0TexReflectSkinnedTech = FX.GetTechniqueByName("Light0TexReflectSkinned");
            Light1TexReflectSkinnedTech = FX.GetTechniqueByName("Light1TexReflectSkinned");
            Light2TexReflectSkinnedTech = FX.GetTechniqueByName("Light2TexReflectSkinned");
            Light3TexReflectSkinnedTech = FX.GetTechniqueByName("Light3TexReflectSkinned");

            Light0TexAlphaClipReflectSkinnedTech = FX.GetTechniqueByName("Light0TexAlphaClipReflectSkinned");
            Light1TexAlphaClipReflectSkinnedTech = FX.GetTechniqueByName("Light1TexAlphaClipReflectSkinned");
            Light2TexAlphaClipReflectSkinnedTech = FX.GetTechniqueByName("Light2TexAlphaClipReflectSkinned");
            Light3TexAlphaClipReflectSkinnedTech = FX.GetTechniqueByName("Light3TexAlphaClipReflectSkinned");

            Light1FogReflectSkinnedTech = FX.GetTechniqueByName("Light1FogReflectSkinned");
            Light2FogReflectSkinnedTech = FX.GetTechniqueByName("Light2FogReflectSkinned");
            Light3FogReflectSkinnedTech = FX.GetTechniqueByName("Light3FogReflectSkinned");

            Light0TexFogReflectSkinnedTech = FX.GetTechniqueByName("Light0TexFogReflectSkinned");
            Light1TexFogReflectSkinnedTech = FX.GetTechniqueByName("Light1TexFogReflectSkinned");
            Light2TexFogReflectSkinnedTech = FX.GetTechniqueByName("Light2TexFogReflectSkinned");
            Light3TexFogReflectSkinnedTech = FX.GetTechniqueByName("Light3TexFogReflectSkinned");

            Light0TexAlphaClipFogReflectSkinnedTech = FX.GetTechniqueByName("Light0TexAlphaClipFogReflectSkinned");
            Light1TexAlphaClipFogReflectSkinnedTech = FX.GetTechniqueByName("Light1TexAlphaClipFogReflectSkinned");
            Light2TexAlphaClipFogReflectSkinnedTech = FX.GetTechniqueByName("Light2TexAlphaClipFogReflectSkinned");
            Light3TexAlphaClipFogReflectSkinnedTech = FX.GetTechniqueByName("Light3TexAlphaClipFogReflectSkinned");

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
            _shadowMap = FX.GetVariableByName("gShadowMap").AsResource();
            _cubeMap = FX.GetVariableByName("gCubeMap").AsResource();

            _boneTransforms = FX.GetVariableByName("gBoneTransforms").AsMatrix();
            
            _shadowTransform = FX.GetVariableByName("gShadowTransform").AsMatrix();

            _ssaoMap = FX.GetVariableByName("gSsaoMap").AsResource();

            _worldViewProjTex = FX.GetVariableByName("gWorldViewProjTex").AsMatrix();

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
            System.Diagnostics.Debug.Assert(lights.Length <= MaxLights, "BasicEffect only supports up to 3 lights");

            for (int i = 0; i < lights.Length && i < MaxLights; i++) {
                var light = lights[i];
                var d = Util.GetArray(light);
                Array.Copy(d, 0, _dirLightsArray, i*DirectionalLight.Stride, DirectionalLight.Stride );
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
        public void SetShadowMap(ShaderResourceView tex) {
            _shadowMap.SetResource(tex);
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

        public void SetBoneTransforms(List<Matrix> bones) {
            for (int i = 0; i < bones.Count && i < MaxBones; i++) {
                _boneTransformsArray[i] = bones[i];
            }
            _boneTransforms.SetMatrixArray(_boneTransformsArray);
        }

        public void SetShadowTransform(Matrix matrix) {
            if (_shadowTransform != null)
                _shadowTransform.SetMatrix(matrix);
        }

        public void SetSsaoMap(ShaderResourceView srv) { _ssaoMap.SetResource(srv); }

        public void SetWorldViewProjTex(Matrix matrix) {
            if ( _worldViewProjTex != null )_worldViewProjTex.SetMatrix(matrix);
        }
    }
}