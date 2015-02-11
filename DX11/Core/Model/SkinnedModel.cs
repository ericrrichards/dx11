using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assimp;
using Core.Vertex;
using SlimDX;
using Device = SlimDX.Direct3D11.Device;

namespace Core.Model {
    public class SkinnedModel : IModel<PosNormalTexTanSkinned> {
        
        private Vector3 _min;
        private Vector3 _max;
        protected internal SceneAnimator Animator { get; private set; }


        public SkinnedModel(Device device, TextureManager texMgr, string filename, string texturePath, bool flipTexY = false) {
           
            
            var importer = new AssimpContext();
        #if DEBUG
            var logstream = new ConsoleLogStream();
            logstream.Attach();
        #endif
            var model = importer.ImportFile(filename, PostProcessSteps.GenerateSmoothNormals | PostProcessSteps.CalculateTangentSpace );
    
            // Load animation data
            Animator = new SceneAnimator();
            Animator.Init(model);

            // create our vertex-to-boneweights lookup
            var vertToBoneWeight = new Dictionary<uint, List<VertexWeight>>();
            // create bounding box extents
            _min = new Vector3(float.MaxValue);
            _max = new Vector3(float.MinValue);

            foreach (var mesh in model.Meshes) {
                ExtractBoneWeightsFromMesh(mesh, vertToBoneWeight);
                var subset = new MeshGeometry.Subset {
                    VertexCount = mesh.VertexCount,
                    VertexStart = Vertices.Count,
                    FaceStart = Indices.Count / 3,
                    FaceCount = mesh.FaceCount
                };
                Subsets.Add(subset);

                var verts = ExtractVertices(mesh, vertToBoneWeight, flipTexY);
                Vertices.AddRange(verts);
                // extract indices and shift them to the proper offset into the combined vertex buffer
                var indices = mesh.GetIndices().Select(i => ((int)i + subset.VertexStart)).ToList();
                Indices.AddRange(indices);

                // extract materials
                var mat = model.Materials[mesh.MaterialIndex];
                var material = mat.ToMaterial();
                Materials.Add(material);

                // extract material textures
                TextureSlot diffuseSlot;
                mat.GetMaterialTexture(TextureType.Diffuse, 0, out diffuseSlot);
                var diffusePath = diffuseSlot.FilePath;
                if (!string.IsNullOrEmpty(diffusePath)) {
                    DiffuseMapSRV.Add(texMgr.CreateTexture(Path.Combine(texturePath, diffusePath)));
                }

                TextureSlot normalSlot;
                mat.GetMaterialTexture(TextureType.Normals, 0, out normalSlot);
                var normalPath = normalSlot.FilePath;
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
            BoundingBox = new BoundingBox(_min, _max);
            
            ModelMesh.SetSubsetTable(Subsets);
            ModelMesh.SetVertices(device, Vertices);
            ModelMesh.SetIndices(device, Indices);
        }

        private IEnumerable<PosNormalTexTanSkinned> ExtractVertices(Mesh mesh, IReadOnlyDictionary<uint, List<VertexWeight>> vertToBoneWeights, bool flipTexY) {
            var verts = new List<PosNormalTexTanSkinned>();
            for (var i = 0; i < mesh.VertexCount; i++) {
                var pos = mesh.HasVertices ? mesh.Vertices[i].ToVector3() : new Vector3();
                _min = Vector3.Minimize(_min, pos);
                _max = Vector3.Maximize(_max, pos);
                
                var norm = mesh.HasNormals ? mesh.Normals[i] : new Vector3D();

                var tan = mesh.HasTangentBasis ? mesh.Tangents[i] : new Vector3D();
                var texC = new Vector3D();
                if (mesh.HasTextureCoords(0)) {
                    var coord = mesh.TextureCoordinateChannels[0][i];
                    if (flipTexY) {
                        coord.Y = -coord.Y;
                    }
                    texC = coord;
                }


                var weights = vertToBoneWeights[(uint) i].Select(w => w.Weight).ToArray();
                var boneIndices = vertToBoneWeights[(uint) i].Select(w => (byte) w.VertexID).ToArray();

                var v = new PosNormalTexTanSkinned(pos, norm.ToVector3(), texC.ToVector2(), tan.ToVector3(), weights.First(), boneIndices);
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
                    if (vertToBoneWeight.ContainsKey((uint) weight.VertexID)) {
                        vertToBoneWeight[(uint) weight.VertexID].Add(new VertexWeight(boneIndex, weight.Weight));
                    } else {
                        vertToBoneWeight[(uint) weight.VertexID] = new List<VertexWeight>(
                            new[] {new VertexWeight(boneIndex, weight.Weight)}
                        );
                    }
                }
            }
        }


        protected override void InitFromMeshData(Device device, GeometryGenerator.MeshData mesh) { throw new System.NotImplementedException(); }

        public override void CreateBox(Device device, float width, float height, float depth) { throw new System.NotImplementedException(); }

        public override void CreateSphere(Device device, float radius, int slices, int stacks) { throw new System.NotImplementedException(); }

        public override void CreateCylinder(Device device, float bottomRadius, float topRadius, float height, int sliceCount, int stackCount) { throw new System.NotImplementedException(); }

        public override void CreateGrid(Device device, float width, float depth, int xVerts, int zVerts) { throw new System.NotImplementedException(); }
        public override void CreateGeosphere(Device device, float radius, GeometryGenerator.SubdivisionCount numSubdivisions) {throw new System.NotImplementedException();}
    }
}