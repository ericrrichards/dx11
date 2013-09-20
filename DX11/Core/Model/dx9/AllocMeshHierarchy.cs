using System.IO;
using System.Linq;
using SlimDX;
using SlimDX.Direct3D9;

namespace Core.Model.dx9 {
    public class AllocMeshHierarchy : DefaultAllocateHierarchy {
        private const int MaxMatrices = 26;
        public AllocMeshHierarchy() { }

        public override Frame CreateFrame(string name) {
            return new FrameEx() { Name = name };
        }
        public override MeshContainer CreateMeshContainer(string name, MeshData meshData, ExtendedMaterial[] materials, EffectInstance[] effectInstances, int[] adjacency, SkinInfo skinInfo) {
            var meshContainer = new CustomMeshContainer();

            meshContainer.Name = name;
            meshContainer.MeshData = meshData;

            meshContainer.SetAdjacency(adjacency);
            meshContainer.SetEffects(effectInstances);
            meshContainer.SetMaterials(materials);
            meshContainer.Textures = new Texture[materials.Length];

            for (int i = 0; i < materials.Length; i++) {
                if (!string.IsNullOrEmpty(materials[i].TextureFileName)) {
                    if (File.Exists(materials[i].TextureFileName)) {
                        meshContainer.Textures[i] = Texture.FromFile(meshData.Mesh.Device, materials[i].TextureFileName);
                    } else {
                        meshContainer.Textures[i] = null;
                    }
                }
            }

            meshContainer.SkinInfo = skinInfo;
            
            meshContainer.OriginalMesh = meshData.Mesh.Clone(meshData.Mesh.Device, meshData.Mesh.CreationOptions, meshData.Mesh.VertexFormat );
            

            meshContainer.BoneOffsets = new Matrix[skinInfo.BoneCount];
            for (int i = 0; i < skinInfo.BoneCount; i++) {
                meshContainer.BoneOffsets[i] = skinInfo.GetBoneOffsetMatrix(i);
            }
            meshContainer.PaletteEntries = meshContainer.SkinInfo.BoneCount;

            int influences;
            BoneCombination[] boneCombinations;

            meshContainer.MeshData.Mesh.Dispose();
            meshContainer.MeshData = new MeshData(meshContainer.SkinInfo.ConvertToIndexedBlendedMesh(meshContainer.OriginalMesh, meshContainer.PaletteEntries, adjacency, out influences, out boneCombinations));

            meshContainer.Influences = influences;
            meshContainer.BoneCombinations = boneCombinations;



            return meshContainer;
        }
    }
}