namespace Core.Model {
    using FX;

    using SlimDX;
    using SlimDX.Direct3D11;

    public class BasicModelInstance {
        public BasicModel Model;
        public Matrix World = Matrix.Identity;
        public Matrix TexTransform = Matrix.Identity;
        public Matrix ShadowTransform = Matrix.Identity;
        public Matrix ToTexSpace = Matrix.Identity;

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
            Effects.NormalMapFX.SetWorldViewProjTex(wvp*ToTexSpace);
            Effects.NormalMapFX.SetShadowTransform(world * ShadowTransform);
            Effects.NormalMapFX.SetTexTransform(TexTransform);
            
            for (int i = 0; i < Model.SubsetCount; i++) {
                Effects.NormalMapFX.SetMaterial(Model.Materials[i]);
                Effects.NormalMapFX.SetDiffuseMap(Model.DiffuseMapSRV[i]);
                Effects.NormalMapFX.SetNormalMap(Model.NormalMapSRV[i]);

                effectPass.Apply(dc);
                Model.ModelMesh.Draw(dc, i);
            }
        }

        public void DrawSsaoDepth(DeviceContext dc, EffectPass pass, Matrix view, Matrix proj) {
            
            var world = World;
            var wit = MathF.InverseTranspose(world);
            var wv = world * view;
            var witv = wit * view;
            var wvp = world * view*proj;

            Effects.SsaoNormalDepthFX.SetWorldView(wv);
            Effects.SsaoNormalDepthFX.SetWorldInvTransposeView(witv);
            Effects.SsaoNormalDepthFX.SetWorldViewProj(wvp);
            Effects.SsaoNormalDepthFX.SetTexTransform(TexTransform);

            pass.Apply(dc);
            for (int i = 0; i < Model.SubsetCount; i++) {
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
            Effects.BasicFX.SetWorldViewProjTex(wvp * ToTexSpace);
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

        public void DrawDisplaced(DeviceContext dc, EffectPass pass, Matrix viewProj) {
            var world = World;
            var wit = MathF.InverseTranspose(world);
            var wvp = world * viewProj;

            Effects.DisplacementMapFX.SetWorld(world);
            Effects.DisplacementMapFX.SetWorldInvTranspose(wit);
            Effects.DisplacementMapFX.SetWorldViewProj(wvp);
            Effects.DisplacementMapFX.SetViewProj(viewProj);
            Effects.DisplacementMapFX.SetWorldViewProjTex(wvp * ToTexSpace);
            Effects.DisplacementMapFX.SetShadowTransform(world * ShadowTransform);
            Effects.DisplacementMapFX.SetTexTransform(TexTransform);

            for (int i = 0; i < Model.SubsetCount; i++) {
                Effects.DisplacementMapFX.SetMaterial(Model.Materials[i]);
                Effects.DisplacementMapFX.SetDiffuseMap(Model.DiffuseMapSRV[i]);
                Effects.DisplacementMapFX.SetNormalMap(Model.NormalMapSRV[i]);

                pass.Apply(dc);
                Model.ModelMesh.Draw(dc, i);
            }
        }
    }
}