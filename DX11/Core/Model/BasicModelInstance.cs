namespace Core.Model {
    using FX;

    using SlimDX;
    using SlimDX.Direct3D11;

    public class BasicModelInstance {
        public BasicModel Model;
        public Matrix World;
        public Matrix TexTransform = Matrix.Identity;
        public Matrix ShadowTransform = Matrix.Identity;
        public BoundingBox BoundingBox {
            get {
                return new BoundingBox(
                    Vector3.TransformCoordinate(Model.BoundingBox.Minimum, World),
                    Vector3.TransformCoordinate(Model.BoundingBox.Maximum, World)
                    );
            }
        }
        public BasicModelInstance() {
            World = Matrix.Identity;
        }
        public void Draw(DeviceContext dc, EffectPass effectPass, Matrix viewProj) {
            var world = World;
            var wit = MathF.InverseTranspose(world);
            var wvp = world * viewProj;

            Effects.NormalMapFX.SetWorld(world);
            Effects.NormalMapFX.SetWorldInvTranspose(wit);
            Effects.NormalMapFX.SetWorldViewProj(wvp);
            Effects.NormalMapFX.SetTexTransform(TexTransform);
            Effects.NormalMapFX.SetShadowTransform(world*ShadowTransform);

            for (int i = 0; i < Model.SubsetCount; i++) {
                Effects.NormalMapFX.SetMaterial(Model.Materials[i]);
                Effects.NormalMapFX.SetDiffuseMap(Model.DiffuseMapSRV[i]);
                Effects.NormalMapFX.SetNormalMap(Model.NormalMapSRV[i]);

                effectPass.Apply(dc);
                Model.ModelMesh.Draw(dc, i);
            }
        }
        public void DrawBasic(DeviceContext dc, EffectPass effectPass, Matrix viewProj) {
            var world = World;
            var wit = MathF.InverseTranspose(world);
            var wvp = world * viewProj;

            Effects.BasicFX.SetWorld(world);
            Effects.BasicFX.SetWorldInvTranspose(wit);
            Effects.BasicFX.SetWorldViewProj(wvp);
            Effects.BasicFX.SetTexTransform(TexTransform);
            Effects.BasicFX.SetShadowTransform(world*ShadowTransform);

            for (int i = 0; i < Model.SubsetCount; i++) {
                Effects.BasicFX.SetMaterial(Model.Materials[i]);
                Effects.BasicFX.SetDiffuseMap(Model.DiffuseMapSRV[i]);

                effectPass.Apply(dc);
                Model.ModelMesh.Draw(dc, i);
            }
        }
        public void DrawShadow(DeviceContext dc, EffectPass effectPass, Matrix viewProj) {
            var world = World;
            var wit = MathF.InverseTranspose(world);
            var wvp = world * viewProj;

            Effects.BuildShadowMapFX.SetWorld(world);
            Effects.BuildShadowMapFX.SetWorldInvTranspose(wit);
            Effects.BuildShadowMapFX.SetWorldViewProj(wvp);

            for (int i = 0; i < Model.SubsetCount; i++) {
                effectPass.Apply(dc);
                Model.ModelMesh.Draw(dc, i);
            }
        }
    }
}