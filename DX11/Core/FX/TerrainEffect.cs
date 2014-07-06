using System;
using System.Collections.Generic;
using System.Linq;
using SlimDX;
using SlimDX.Direct3D11;

using Debug = System.Diagnostics.Debug;

namespace Core.FX {
    public class TerrainEffect : Effect {
        public EffectTechnique Light1Tech { get; private set; }
        public EffectTechnique Light2Tech { get; private set; }
        public EffectTechnique Light3Tech { get; private set; }
        public EffectTechnique Light1FogTech { get; private set; }
        public EffectTechnique Light2FogTech { get; private set; }
        public EffectTechnique Light3FogTech { get; private set; }

        public EffectTechnique Light1ShadowTech { get; private set; }
        public EffectTechnique Light2ShadowTech { get; private set; }
        public EffectTechnique Light3ShadowTech { get; private set; }
        public EffectTechnique Light1FogShadowTech { get; private set; }
        public EffectTechnique Light2FogShadowTech { get; private set; }
        public EffectTechnique Light3FogShadowTech { get; private set; }

        public EffectTechnique Light1TechNT { get; private set; }
        public EffectTechnique Light2TechNT { get; private set; }
        public EffectTechnique Light3TechNT { get; private set; }
        public EffectTechnique Light1FogTechNT { get; private set; }
        public EffectTechnique Light2FogTechNT { get; private set; }
        public EffectTechnique Light3FogTechNT { get; private set; }
        public EffectTechnique NormalDepthTech { get; private set; }
        public EffectTechnique TessBuildShadowMapTech { get; private set; }

        private readonly EffectMatrixVariable _viewProj;
        private readonly EffectVectorVariable _eyePosW;
        private readonly EffectVectorVariable _fogColor;
        private readonly EffectScalarVariable _fogStart;
        private readonly EffectScalarVariable _fogRange;

        private readonly EffectVariable _dirLights;
        public const int MaxLights = 3;
        private readonly byte[] _dirLightsArray = new byte[DirectionalLight.Stride * MaxLights];

        private readonly EffectVariable _mat;
        private readonly EffectScalarVariable _minDist;
        private readonly EffectScalarVariable _maxDist;
        private readonly EffectScalarVariable _minTess;
        private readonly EffectScalarVariable _maxTess;
        private readonly EffectScalarVariable _texelCellSpaceU;
        private readonly EffectScalarVariable _texelCellSpaceV;
        private readonly EffectScalarVariable _worldCellSpace;
        private readonly EffectVectorVariable _worldFrustumPlanes;

        private readonly EffectResourceVariable _layerMapArray;
        private readonly EffectResourceVariable _blendMap;
        private readonly EffectResourceVariable _heightMap;

        private readonly EffectMatrixVariable _view;
        private readonly EffectResourceVariable _ambientMap;
        private readonly EffectMatrixVariable _viewProjTex;
        private readonly EffectMatrixVariable _shadowTransform;
        private readonly EffectResourceVariable _shadowMap;
        private readonly EffectResourceVariable _walkMap;
        private readonly EffectResourceVariable _unwalkableSRV;

        public TerrainEffect(Device device, string filename) : base(device, filename) {
            Light1Tech = FX.GetTechniqueByName("Light1");
            Light2Tech = FX.GetTechniqueByName("Light2");
            Light3Tech = FX.GetTechniqueByName("Light3");
            Light1FogTech = FX.GetTechniqueByName("Light1Fog");
            Light2FogTech = FX.GetTechniqueByName("Light2Fog");
            Light3FogTech = FX.GetTechniqueByName("Light3Fog");

            Light1ShadowTech = FX.GetTechniqueByName("Light1Shadow");
            Light2ShadowTech = FX.GetTechniqueByName("Light2Shadow");
            Light3ShadowTech = FX.GetTechniqueByName("Light3Shadow");
            Light1FogShadowTech = FX.GetTechniqueByName("Light1FogShadow");
            Light2FogShadowTech = FX.GetTechniqueByName("Light2FogShadow");
            Light3FogShadowTech = FX.GetTechniqueByName("Light3FogShadow");

            Light1TechNT = FX.GetTechniqueByName("Light1NT");
            Light2TechNT = FX.GetTechniqueByName("Light2NT");
            Light3TechNT = FX.GetTechniqueByName("Light3NT");
            Light1FogTechNT = FX.GetTechniqueByName("Light1FogNT");
            Light2FogTechNT = FX.GetTechniqueByName("Light2FogNT");
            Light3FogTechNT = FX.GetTechniqueByName("Light3FogNT");

            NormalDepthTech = FX.GetTechniqueByName("NormalDepth");
            TessBuildShadowMapTech = FX.GetTechniqueByName("TessBuildShadowMapTech");

            _viewProj = FX.GetVariableByName("gViewProj").AsMatrix();
            _eyePosW = FX.GetVariableByName("gEyePosW").AsVector();

            _fogColor = FX.GetVariableByName("gFogColor").AsVector();
            _fogStart = FX.GetVariableByName("gFogStart").AsScalar();
            _fogRange = FX.GetVariableByName("gFogRange").AsScalar();

            _dirLights = FX.GetVariableByName("gDirLights");
            _mat = FX.GetVariableByName("gMaterial");

            _minDist = FX.GetVariableByName("gMinDist").AsScalar();
            _maxDist = FX.GetVariableByName("gMaxDist").AsScalar();
            _minTess = FX.GetVariableByName("gMinTess").AsScalar();
            _maxTess = FX.GetVariableByName("gMaxTess").AsScalar();
            _texelCellSpaceU = FX.GetVariableByName("gTexelCellSpaceU").AsScalar();
            _texelCellSpaceV = FX.GetVariableByName("gTexelCellSpaceV").AsScalar();
            _worldCellSpace = FX.GetVariableByName("gWorldCellSpace").AsScalar();
            _worldFrustumPlanes = FX.GetVariableByName("gWorldFrustumPlanes").AsVector();

            _layerMapArray = FX.GetVariableByName("gLayerMapArray").AsResource();
            _blendMap = FX.GetVariableByName("gBlendMap").AsResource();
            _heightMap = FX.GetVariableByName("gHeightMap").AsResource();

            _view = FX.GetVariableByName("gView").AsMatrix();
            _ambientMap = FX.GetVariableByName("gSsaoMap").AsResource();
            _viewProjTex = FX.GetVariableByName("gViewProjTex").AsMatrix();
            _shadowTransform = FX.GetVariableByName("gShadowTransform").AsMatrix();
            _shadowMap = FX.GetVariableByName("gShadowMap").AsResource();
            _walkMap = FX.GetVariableByName("gWalkMap").AsResource();
            _unwalkableSRV = FX.GetVariableByName("gUnwalkable").AsResource();
        }
        public void SetView(Matrix m) {
            _view.SetMatrix(m);
        }

        public void SetViewProj(Matrix m) {
            _viewProj.SetMatrix(m);
        }
        public void SetEyePosW(Vector3 v) {
            _eyePosW.Set(v);
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
        public void SetShadowTransform(Matrix matrix) {
            if (_shadowTransform != null)
                _shadowTransform.SetMatrix(matrix);
        }
        public void SetShadowMap(ShaderResourceView tex) {
            _shadowMap.SetResource(tex);
        }

        public void SetDirLights(DirectionalLight[] lights) {
            Debug.Assert(lights.Length <= MaxLights, "BasicEffect only supports up to 3 lights");

            for (var i = 0; i < lights.Length && i < MaxLights; i++) {
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

        public void SetMinDist(float f) {
            _minDist.Set(f);
        }
        public void SetMaxDist(float f) {
            _maxDist.Set(f);
        }
        public void SetMinTess(float f) {
            _minTess.Set(f);
        }
        public void SetMaxTess(float f) {
            _maxTess.Set(f);
        }
        public void SetTexelCellSpaceU(float f) {
            _texelCellSpaceU.Set(f);
        }
        public void SetTexelCellSpaceV(float f) {
            _texelCellSpaceV.Set(f);
        }
        public void SetWorldCellSpace(float f) {
            _worldCellSpace.Set(f);
        }
        public void SetWorldFrustumPlanes(Plane[] planes) {
            Debug.Assert(planes.Length == 6);
            _worldFrustumPlanes.Set(planes.Select(p=>new Vector4(p.Normal, p.D)).ToArray());
        }

        public void SetLayerMapArray(ShaderResourceView tex) {
            _layerMapArray.SetResource(tex);
        }
        public void SetBlendMap(ShaderResourceView tex) {
            _blendMap.SetResource(tex);
        }
        public void SetHeightMap(ShaderResourceView tex) {
            _heightMap.SetResource(tex);
        }

        public void SetSsaoMap(ShaderResourceView srv) {
            _ambientMap.SetResource(srv);
        }

        public void SetViewProjTex(Matrix matrix) {
            _viewProjTex.SetMatrix(matrix);
        }

        public void SetWalkMap(ShaderResourceView walkableTiles) {
            if (_walkMap != null) {
                _walkMap.SetResource(walkableTiles);
            } else {
                Console.WriteLine("walkmap not defined in shader");
            }
        }

        public void SetUnwalkableTex(ShaderResourceView shaderResourceView) {
            if (_unwalkableSRV != null) {
                _unwalkableSRV.SetResource(shaderResourceView);
            } else {
                Console.WriteLine("unwalkableSRV not defined in shader");
            }
        
        }
        
    }
}