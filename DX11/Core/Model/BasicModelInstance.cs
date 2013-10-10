namespace Core.Model {
    using Core.FX;

    using SlimDX;
    using SlimDX.Direct3D11;

    public struct BasicModelInstance {
        public BasicModel Model;
        public Matrix World;
        public BoundingBox BoundingBox {
            get {
                return new BoundingBox(
                    Vector3.TransformCoordinate(Model.BoundingBox.Minimum, World),
                    Vector3.TransformCoordinate(Model.BoundingBox.Maximum, World)
                    );
            }
        }
        public void Draw(DeviceContext dc, EffectPass effectPass, Matrix viewProj) {
            var world = World;
            var wit = MathF.InverseTranspose(world);
            var wvp = world * viewProj;

            Effects.NormalMapFX.SetWorld(world);
            Effects.NormalMapFX.SetWorldInvTranspose(wit);
            Effects.NormalMapFX.SetWorldViewProj(wvp);
            Effects.NormalMapFX.SetTexTransform(Matrix.Identity);

            for (int i = 0; i < Model.SubsetCount; i++) {
                Effects.NormalMapFX.SetMaterial(Model.Materials[i]);
                Effects.NormalMapFX.SetDiffuseMap(Model.DiffuseMapSRV[i]);
                Effects.NormalMapFX.SetNormalMap(Model.NormalMapSRV[i]);

                effectPass.Apply(dc);
                Model.ModelMesh.Draw(dc, i);
            }
        }

    }
}