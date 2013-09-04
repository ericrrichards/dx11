using System.Collections.Generic;
using System.Linq;
using SlimDX;
using SlimDX.Direct3D11;

using Debug = System.Diagnostics.Debug;

namespace Core.FX {
    public class TerrainEffect : Effect {
        public EffectTechnique Light1Tech;
        public EffectTechnique Light2Tech;
        public EffectTechnique Light3Tech;
        public EffectTechnique Light1FogTech;
        public EffectTechnique Light2FogTech;
        public EffectTechnique Light3FogTech;

        private readonly EffectMatrixVariable _viewProj;
        private readonly EffectVectorVariable _eyePosW;
        private readonly EffectVectorVariable _fogColor;
        private readonly EffectScalarVariable _fogStart;
        private readonly EffectScalarVariable _fogRange;
        private readonly EffectVariable _dirLights;
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


        public TerrainEffect(Device device, string filename) : base(device, filename) {
            Light1Tech = FX.GetTechniqueByName("Light1");
            Light2Tech = FX.GetTechniqueByName("Light2");
            Light3Tech = FX.GetTechniqueByName("Light3");
            Light1FogTech = FX.GetTechniqueByName("Light1Fog");
            Light2FogTech = FX.GetTechniqueByName("Light2Fog");
            Light3FogTech = FX.GetTechniqueByName("Light3Fog");

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
        public void SetDirLights(DirectionalLight[] lights) {
            Debug.Assert(lights.Length <= 3, "TerrainEffect only supports up to 3 lights");
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


    }
}