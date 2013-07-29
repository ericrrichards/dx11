using System;
using System.Collections.Generic;
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

        
        private EffectMatrixVariable WorldViewProj;
        private EffectMatrixVariable World;
        private EffectMatrixVariable WorldInvTranspose;
        private EffectVectorVariable EyePosW;
        private EffectVariable DirLights;
        private EffectVariable Mat;

        private EffectResourceVariable DiffuseMap;
        private EffectMatrixVariable TexTransform;

        public BasicEffect(Device device, string filename) : base(device, filename) {
            Light1Tech = FX.GetTechniqueByName("Light1");
            Light2Tech = FX.GetTechniqueByName("Light2");
            Light3Tech = FX.GetTechniqueByName("Light3");

            Light0TexTech = FX.GetTechniqueByName("Light0Tex");
            Light1TexTech = FX.GetTechniqueByName("Light1Tex");
            Light2TexTech = FX.GetTechniqueByName("Light2Tex");
            Light3TexTech = FX.GetTechniqueByName("Light3Tex");

            WorldViewProj = FX.GetVariableByName("gWorldViewProj").AsMatrix();
            World = FX.GetVariableByName("gWorld").AsMatrix();
            WorldInvTranspose = FX.GetVariableByName("gWorldInvTranspose").AsMatrix();
            TexTransform = FX.GetVariableByName("gTexTransform").AsMatrix();
            EyePosW = FX.GetVariableByName("gEyePosW").AsVector();
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
