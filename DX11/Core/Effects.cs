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
                Util.ReleaseCom(compiledShader);
            }
        }
        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Util.ReleaseCom(FX);
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

        public EffectMatrixVariable WorldViewProj;
        public EffectMatrixVariable World;
        public EffectMatrixVariable WorldInvTranspose;
        public EffectVectorVariable EyePosW;
        public EffectVariable DirLights;
        public EffectVariable Mat;

        public BasicEffect(Device device, string filename) : base(device, filename) {
            Light1Tech = FX.GetTechniqueByName("Light1");
            Light2Tech = FX.GetTechniqueByName("Light2");
            Light3Tech = FX.GetTechniqueByName("Light3");
            WorldViewProj = FX.GetVariableByName("gWorldViewProj").AsMatrix();
            World = FX.GetVariableByName("gWorld").AsMatrix();
            WorldInvTranspose = FX.GetVariableByName("gWorldInvTranspose").AsMatrix();
            EyePosW = FX.GetVariableByName("gEyePosW").AsVector();
            DirLights = FX.GetVariableByName("gDirLights");
            Mat = FX.GetVariableByName("gMaterial");
            
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
    }

    public class Effects {
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
