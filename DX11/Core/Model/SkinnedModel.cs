using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assimp;
using Core.Vertex;
using SlimDX.Direct3D11;

using Device = SlimDX.Direct3D11.Device;

namespace Core.Model {
    public class SkinnedModel : DisposableClass {
        private MeshGeometry _modelMesh;
        private readonly List<MeshGeometry.Subset> _subsets;
        private readonly List<PosNormalTexTanSkinned> _vertices;
        private readonly List<short> _indices;
        private bool _disposed;

        public int SubsetCount { get { return _subsets.Count; } }
        public List<Material> Materials { get; private set; }
        public List<ShaderResourceView> DiffuseMapSRV { get; private set; }
        public MeshGeometry ModelMesh { get { return _modelMesh; } }
        public List<ShaderResourceView> NormalMapSRV { get; private set; }
        protected internal SceneAnimator Animator { get; private set; }


        public SkinnedModel(Device device, TextureManager texMgr, string filename, string texturePath, bool flipTexY = false) {
            _subsets = new List<MeshGeometry.Subset>();
            _vertices = new List<PosNormalTexTanSkinned>();
            _indices = new List<short>();
            DiffuseMapSRV = new List<ShaderResourceView>();
            NormalMapSRV = new List<ShaderResourceView>();
            Materials = new List<Material>();
            
            var importer = new AssimpImporter();
#if DEBUG

            importer.AttachLogStream(new ConsoleLogStream());
            importer.VerboseLoggingEnabled = true;
#endif
            var model = importer.ImportFile(filename, PostProcessSteps.GenerateSmoothNormals | PostProcessSteps.CalculateTangentSpace );


            Animator = new SceneAnimator();
            Animator.Init(model);

            // create our vertex-to-boneweights lookup
            var vertToBoneWeight = new Dictionary<uint, List<VertexWeight>>();
            
            foreach (var mesh in model.Meshes) {
                ExtractBoneWeightsFromMesh(mesh, vertToBoneWeight);
                var subset = new MeshGeometry.Subset {
                    VertexCount = mesh.VertexCount,
                    VertexStart = _vertices.Count,
                    FaceStart = _indices.Count / 3,
                    FaceCount = mesh.FaceCount
                };
                _subsets.Add(subset);

                var verts = ExtractVertices(mesh, vertToBoneWeight, flipTexY);
                _vertices.AddRange(verts);
                // extract indices and shift them to the proper offset into the combined vertex buffer
                var indices = mesh.GetIndices().Select(i => (short)(i + (uint)subset.VertexStart)).ToList();
                _indices.AddRange(indices);

                // extract materials
                var mat = model.Materials[mesh.MaterialIndex];
                var material = mat.ToMaterial();

                Materials.Add(material);
                // extract material textures
                var diffusePath = mat.GetTexture(TextureType.Diffuse, 0).FilePath;
                if (!string.IsNullOrEmpty(diffusePath)) {
                    DiffuseMapSRV.Add(texMgr.CreateTexture(Path.Combine(texturePath, diffusePath)));
                }
                var normalPath = mat.GetTexture(TextureType.Normals, 0).FilePath;
                if (!string.IsNullOrEmpty(normalPath)) {
                    NormalMapSRV.Add(texMgr.CreateTexture(Path.Combine(texturePath, normalPath)));
                } else {
                    // for models created without a normal map baked, we'll check for a texture with the same 
                    // filename as the diffure texture, and _nmap suffixed
                    // this lets us add our own normal maps easily
                    var normalExt = Path.GetExtension(diffusePath);
                    normalPath = Path.GetFileNameWithoutExtension(diffusePath) + "_nmap" + normalExt;
                    if (File.Exists(Path.Combine(texturePath, normalPath))) {
                        NormalMapSRV.Add(texMgr.CreateTexture(Path.Combine(texturePath, normalPath)));
                    }
                }
            }

            _modelMesh = new MeshGeometry();
            _modelMesh.SetSubsetTable(_subsets);
            _modelMesh.SetVertices(device, _vertices);
            _modelMesh.SetIndices(device, _indices);
        }

        private static IEnumerable<PosNormalTexTanSkinned> ExtractVertices(Mesh mesh, IReadOnlyDictionary<uint, List<VertexWeight>> vertToBoneWeights, bool flipTexY) {
            var verts = new List<PosNormalTexTanSkinned>();
            for (var i = 0; i < mesh.VertexCount; i++) {
                var pos = mesh.HasVertices ? mesh.Vertices[i] : new Vector3D();
                var norm = mesh.HasNormals ? mesh.Normals[i] : new Vector3D();

                var tan = mesh.HasTangentBasis ? mesh.Tangents[i] : new Vector3D();
                var texC = new Vector3D();
                if (mesh.HasTextureCoords(0)) {
                    var coord = mesh.GetTextureCoords(0)[i];
                    if (flipTexY) {
                        coord.Y = -coord.Y;
                    }
                    texC = coord;
                }


                var weights = vertToBoneWeights[(uint) i].Select(w => w.Weight).ToArray();
                var boneIndices = vertToBoneWeights[(uint) i].Select(w => (byte) w.VertexID).ToArray();

                var v = new PosNormalTexTanSkinned(pos.ToVector3(), norm.ToVector3(), texC.ToVector2(), tan.ToVector3(), weights.First(), boneIndices);
                verts.Add(v);
            }
            return verts;
        }

        private void ExtractBoneWeightsFromMesh(Mesh mesh, IDictionary<uint, List<VertexWeight>> vertToBoneWeight) {
            foreach (var bone in mesh.Bones) {
                var boneIndex = Animator.GetBoneIndex(bone.Name);
                // bone weights are recorded per bone in assimp, with each bone containing a list of the vertices influenced by it
                // we really want the reverse mapping, i.e. lookup the vertexID and get the bone id and weight
                // We'll support up to 4 bones per vertex, so we need a list of weights for each vertex
                foreach (var weight in bone.VertexWeights) {
                    if (vertToBoneWeight.ContainsKey(weight.VertexID)) {
                        vertToBoneWeight[weight.VertexID].Add(new VertexWeight((uint) boneIndex, weight.Weight));
                    } else {
                        vertToBoneWeight[weight.VertexID] = new List<VertexWeight>(new[] {new VertexWeight((uint) boneIndex, weight.Weight)});
                    }
                }
            }
        }


        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Util.ReleaseCom(ref _modelMesh);
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }
    }
}