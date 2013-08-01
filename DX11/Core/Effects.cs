using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core {
    using System.IO;
    using System.Windows.Forms;

    using SlimDX;
    using SlimDX.D3DCompiler;
    using SlimDX.Direct3D11;

    public abstract class Effect : DisposableClass {
        protected SlimDX.Direct3D11.Effect FX;
        private bool _disposed;
        protected Effect(Device device, string filename) {
            ShaderBytecode compiledShader = null;
            try {
                compiledShader = new ShaderBytecode(new DataStream(File.ReadAllBytes(filename), false, false));
                FX = new SlimDX.Direct3D11.Effect(device, compiledShader);
            } catch (Exception ex) {
                MessageBox.Show(ex.Message);
            } finally {
                Util.ReleaseCom(ref compiledShader);
            }
        }
        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Util.ReleaseCom(ref FX);
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }
    }

    public class BasicEffect : Effect {
        public EffectTechnique Light1Tech;
        public EffectTechnique Light2Tech;
        public EffectTechnique Light3Tech;

        public EffectTechnique Light0TexTech;
        public EffectTechnique Light1TexTech;
        public EffectTechnique Light2TexTech;
        public EffectTechnique Light3TexTech;

        public EffectTechnique Light0TexAlphaClipTech;
        public EffectTechnique Light1TexAlphaClipTech;
        public EffectTechnique Light2TexAlphaClipTech;
        public EffectTechnique Light3TexAlphaClipTech;

        public EffectTechnique Light1FogTech;
        public EffectTechnique Light2FogTech;
        public EffectTechnique Light3FogTech;

        public EffectTechnique Light0TexFogTech;
        public EffectTechnique Light1TexFogTech;
        public EffectTechnique Light2TexFogTech;
        public EffectTechnique Light3TexFogTech;

        public EffectTechnique Light0TexAlphaClipFogTech;
        public EffectTechnique Light1TexAlphaClipFogTech;
        public EffectTechnique Light2TexAlphaClipFogTech;
        public EffectTechnique Light3TexAlphaClipFogTech;

        
        private EffectMatrixVariable WorldViewProj;
        private EffectMatrixVariable World;
        private EffectMatrixVariable WorldInvTranspose;
        private EffectMatrixVariable TexTransform;
        private EffectVectorVariable EyePosW;
        private EffectVectorVariable FogColor;
        private EffectScalarVariable FogStart;
        private EffectScalarVariable FogRange;
        private EffectVariable DirLights;
        private EffectVariable Mat;

        private EffectResourceVariable DiffuseMap;
        

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

            WorldViewProj = FX.GetVariableByName("gWorldViewProj").AsMatrix();
            World = FX.GetVariableByName("gWorld").AsMatrix();
            WorldInvTranspose = FX.GetVariableByName("gWorldInvTranspose").AsMatrix();
            TexTransform = FX.GetVariableByName("gTexTransform").AsMatrix();
            EyePosW = FX.GetVariableByName("gEyePosW").AsVector();

            FogColor = FX.GetVariableByName("gFogColor").AsVector();
            FogStart = FX.GetVariableByName("gFogStart").AsScalar();
            FogRange = FX.GetVariableByName("gFogRange").AsScalar();

            DirLights = FX.GetVariableByName("gDirLights");
            Mat = FX.GetVariableByName("gMaterial");
            DiffuseMap = FX.GetVariableByName("gDiffuseMap").AsResource();
            
        }
        public void SetWorldViewProj(Matrix m) {
            WorldViewProj.SetMatrix(m);
        }
        public void SetWorld(Matrix m) {
            World.SetMatrix(m);
        }
        public void SetWorldInvTranspose(Matrix m) {
            WorldInvTranspose.SetMatrix(m);
        }
        public void SetEyePosW(Vector3 v) {
            EyePosW.Set(v);
        }
        public void SetDirLights(DirectionalLight[] lights) {
            System.Diagnostics.Debug.Assert(lights.Length <= 3, "BasicEffect only supports up to 3 lights");
            var array = new List<byte>();
            foreach (var light in lights) {
                var d = Util.GetArray(light);
                array.AddRange(d);
            }

            DirLights.SetRawValue(new DataStream(array.ToArray(), false, false), array.Count);
        }
        public void SetMaterial(Material m) {
            var d = Util.GetArray(m);
            Mat.SetRawValue(new DataStream(d, false, false), d.Length);
        }

        public void SetTexTransform(Matrix m) {
            TexTransform.SetMatrix(m);
        }

        public void SetDiffuseMap(ShaderResourceView tex) {
            DiffuseMap.SetResource(tex);
        }

        public void SetFogColor(Color4 c) {
            FogColor.Set(c);
        }
        public void SetFogStart(float f) {
            FogStart.Set(f);
        }
        public void SetFogRange(float f) {
            FogRange.Set(f);
        }
    }

    public static class Effects {
        public static void InitAll(Device device) {
            BasicFX = new BasicEffect(device, "FX/Basic.fxo");
        }
        public static void DestroyAll() {
            BasicFX.Dispose();
            BasicFX = null;
        }

        public static BasicEffect BasicFX;
    }
}
