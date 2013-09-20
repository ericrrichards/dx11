using SlimDX;
using SlimDX.Direct3D9;

namespace Core.Model.dx9 {
    public class CustomMeshContainer : MeshContainer {
        public Texture[] Textures { get; set; }
        public Mesh OriginalMesh { get; set; }

        public FrameEx[] BoneMatricesLookup { get; set; }
        public Matrix[] BoneOffsets { get; set; }
        public BoneCombination[] BoneCombinations { get; set; }
        public int Influences { get; set; }
        public int PaletteEntries { get; set; }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                foreach (var texture in Textures) {
                    texture.Dispose();
                }
                OriginalMesh.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}