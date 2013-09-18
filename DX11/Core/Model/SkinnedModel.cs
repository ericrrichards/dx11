using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assimp;
using Core.Vertex;
using SlimDX;
using SlimDX.Direct3D11;

namespace Core.Model {
    public class SkinnedModel : DisposableClass {
        private MeshGeometry _modelMesh;
        private SkinnedData _skinnedData;
        private List<MeshGeometry.Subset> _subsets;
        private List<PosNormalTexTanSkinned> _vertices;
        private List<short> _indices;
        private bool _disposed;

        public int SubsetCount { get { return _subsets.Count; } }
        public List<Material> Materials { get; private set; }
        public List<ShaderResourceView> DiffuseMapSRV { get; private set; }
        public MeshGeometry ModelMesh { get { return _modelMesh; } }
        public List<ShaderResourceView> NormalMapSRV { get; private set; }
        public SkinnedData SkinnedData { get { return _skinnedData; } }


        public SkinnedModel(Device device, TextureManager texMgr, string filename, string texturePath, bool flipTexY = false, bool flipWinding = false) {
            _subsets = new List<MeshGeometry.Subset>();
            _vertices = new List<PosNormalTexTanSkinned>();
            _indices = new List<short>();
            DiffuseMapSRV = new List<ShaderResourceView>();
            NormalMapSRV = new List<ShaderResourceView>();
            Materials = new List<Material>();
            _skinnedData = new SkinnedData();

            var importer = new AssimpImporter();
            var model = importer.ImportFile(filename, PostProcessSteps.GenerateSmoothNormals | PostProcessSteps.CalculateTangentSpace | (flipWinding ? PostProcessSteps.FlipWindingOrder : PostProcessSteps.None));
            _modelMesh = new MeshGeometry();

            var bones = ExtractBones(model.RootNode);

            var boneOffsets = bones.Select(b => b.Transform.ToMatrix()).ToList();
            var boneHierarchy = new List<int>();
            var boneNameToIndex = new Dictionary<string, uint>();
            var k = 0;
            foreach (var bone in bones) {
                boneHierarchy.Add(bones.IndexOf(bone.Parent));
                boneNameToIndex[bone.Name] = (uint) k++;
            }
            
            var vertToBoneWeight = new Dictionary<uint, List<VertexWeight>>();



            var animations = new Dictionary<string, AnimationClip>();
            var verts = new List<PosNormalTexTanSkinned>();
            
            for (int s = 0; s < model.Meshes.Length; s++) {

                var mesh = model.Meshes[s];
                

                foreach (var bone in mesh.Bones) {
                    var boneIndex = boneNameToIndex[bone.Name];
                    foreach (var weight in bone.VertexWeights) {
                        if (vertToBoneWeight.ContainsKey(weight.VertexID)) {
                            vertToBoneWeight[weight.VertexID].Add(new VertexWeight(boneIndex, weight.Weight));
                        } else {
                            vertToBoneWeight[weight.VertexID] = new List<VertexWeight>(new[] { new VertexWeight(boneIndex, weight.Weight) });
                        }
                    }
                }
                
                
                foreach (var animation in model.Animations) {
                    var key = animation.Name;
                    var clip = new AnimationClip();
                    
                    BoneAnimation[] boneAnims = new BoneAnimation[animation.NodeAnimationChannelCount];
                    foreach (var nodeAnimationChannel in animation.NodeAnimationChannels) {
                        if (!boneNameToIndex.ContainsKey(nodeAnimationChannel.NodeName)) {
                            continue;
                        }
                        var boneIndex = boneNameToIndex[nodeAnimationChannel.NodeName];
                        boneAnims[boneIndex] = new BoneAnimation();

                        var keyFrames = new List<Keyframe>();
                        for (int i = 0; i < nodeAnimationChannel.PositionKeyCount; i++) {
                            var kf = new Keyframe();
                            kf.TimePos = (float) nodeAnimationChannel.PositionKeys[i].Time;
                            kf.Translation = nodeAnimationChannel.PositionKeys[i].Value.ToVector3();
                            kf.Scale = nodeAnimationChannel.ScalingKeys[i].Value.ToVector3();
                            kf.RotationQuat = nodeAnimationChannel.RotationKeys[i].Value.ToQuat();
                            keyFrames.Add(kf);
                        }
                        boneAnims[boneIndex].SetKeyFrames(keyFrames);
                    }
                    clip.SetBoneAnimations(boneAnims.Where(b=>b!=null).ToList());
                    animations[key] = clip;
                }

                


                var subset = new MeshGeometry.Subset {
                    Id = s,
                    VertexCount = mesh.VertexCount,
                    VertexStart = _vertices.Count,
                    FaceStart = _indices.Count / 3,
                    FaceCount = mesh.FaceCount
                };
                _subsets.Add(subset);
                for (int i = 0; i < mesh.VertexCount; i++) {
                    var pos = mesh.HasVertices ? mesh.Vertices[i] : new Vector3D();
                    var norm = mesh.HasNormals ? mesh.Normals[i] : new Vector3D();
                    if (flipWinding) {
                        norm = -norm;
                    }
                    var texC = new Vector3D();
                    if (mesh.HasTextureCoords(0)) {
                        var coord = mesh.GetTextureCoords(0)[i];
                        if (flipTexY) {
                            coord.Y = -coord.Y;
                        }
                        texC = coord;
                    }

                    var tan = mesh.HasTangentBasis ? mesh.Tangents[i] : new Vector3D();
                    var weights = vertToBoneWeight[(uint)i].Select(w => w.Weight).ToArray();
                    var boneIndices = vertToBoneWeight[(uint)i].Select(w => w.VertexID).ToArray();

                    var v = new PosNormalTexTanSkinned(pos.ToVector3(), norm.ToVector3(), texC.ToVector2(), new Vector4(tan.ToVector3(), 0), weights, boneIndices);
                    verts.Add(v);
                }
                _vertices.AddRange(verts);
                var indices = mesh.GetIndices().Select(i => (short)(i + (uint)subset.VertexStart)).ToList();
                _indices.AddRange(indices);

                var mat = model.Materials[mesh.MaterialIndex];
                var material = mat.ToMaterial();

                Materials.Add(material);

                var diffusePath = mat.GetTexture(TextureType.Diffuse, 0).FilePath;
                if (!string.IsNullOrEmpty(diffusePath)) {
                    DiffuseMapSRV.Add(texMgr.CreateTexture(Path.Combine(texturePath, diffusePath)));
                }
                var normalPath = mat.GetTexture(TextureType.Normals, 0).FilePath;
                if (!string.IsNullOrEmpty(normalPath)) {
                    NormalMapSRV.Add(texMgr.CreateTexture(Path.Combine(texturePath, normalPath)));
                } else {
                    var normalExt = Path.GetExtension(diffusePath);
                    normalPath = Path.GetFileNameWithoutExtension(diffusePath) + "_nmap" + normalExt;
                    if (File.Exists(Path.Combine(texturePath, normalPath))) {
                        NormalMapSRV.Add(texMgr.CreateTexture(Path.Combine(texturePath, normalPath)));
                    }
                }

            }
            _skinnedData.Set(boneHierarchy.ToList(), boneOffsets.ToList(), animations);


            _modelMesh.SetSubsetTable(_subsets);
            _modelMesh.SetVertices(device, _vertices);
            _modelMesh.SetIndices(device, _indices);



        }

        private static List<Node> ExtractBones(Node rootNode) {
            var ret = new List<Node>();
            
            if (!string.IsNullOrEmpty(rootNode.Name) && rootNode.MeshCount == 0) {
                Console.WriteLine(rootNode.Name);
                if (!rootNode.Name.StartsWith("$")) {
                    ret.Add(rootNode);
                }
                if (rootNode.HasChildren) {
                    foreach (var child in rootNode.Children) {
                        ret.AddRange(ExtractBones(child));
                    }
                }
            }
            return ret;
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

    public class SkinnedModelInstance {
        public SkinnedModel Model;
        public float TimePos;
        public string ClipName;
        public Matrix World;
        public List<Matrix> FinalTransforms;
        public void Update(float dt) {
            TimePos += dt;
            FinalTransforms = Model.SkinnedData.GetFinalTransforms(ClipName, TimePos);
            if (TimePos > Model.SkinnedData.GetClipEndTime(ClipName)) {
                TimePos = 0.0f;
            }
        }
    }
}