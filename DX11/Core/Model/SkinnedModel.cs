using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assimp;
using Core.Camera;
using Core.Vertex;
using SlimDX;
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



        public SkinnedModel(Device device, TextureManager texMgr, string filename, string texturePath, bool flipTexY = false, bool flipWinding = false) {
            _subsets = new List<MeshGeometry.Subset>();
            _vertices = new List<PosNormalTexTanSkinned>();
            _indices = new List<short>();
            DiffuseMapSRV = new List<ShaderResourceView>();
            NormalMapSRV = new List<ShaderResourceView>();
            Materials = new List<Material>();

            Animator = new SceneAnimator();


            var importer = new AssimpImporter();
            importer.AttachLogStream(new ConsoleLogStream());
            importer.VerboseLoggingEnabled = true;
            var model = importer.ImportFile(filename, PostProcessSteps.GenerateSmoothNormals | PostProcessSteps.CalculateTangentSpace | (flipWinding ? PostProcessSteps.FlipWindingOrder : PostProcessSteps.None));
            _modelMesh = new MeshGeometry();


            Animator.Init(model);

            var vertToBoneWeight = new Dictionary<uint, List<VertexWeight>>();

            var verts = new List<PosNormalTexTanSkinned>();

            for (int s = 0; s < model.Meshes.Length; s++) {

                var mesh = model.Meshes[s];


                foreach (var bone in mesh.Bones) {
                    var boneIndex = Animator.GetBoneIndex(bone.Name);

                    foreach (var weight in bone.VertexWeights) {
                        if (vertToBoneWeight.ContainsKey(weight.VertexID)) {
                            vertToBoneWeight[weight.VertexID].Add(new VertexWeight((uint)boneIndex, weight.Weight));
                        } else {
                            vertToBoneWeight[weight.VertexID] = new List<VertexWeight>(new[] { new VertexWeight((uint)boneIndex, weight.Weight) });
                        }
                    }
                }
                var subset = new MeshGeometry.Subset {

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
                    var tan = mesh.HasTangentBasis ? mesh.Tangents[i] : new Vector3D();
                    var texC = new Vector3D();
                    if (mesh.HasTextureCoords(0)) {
                        var coord = mesh.GetTextureCoords(0)[i];
                        if (flipTexY) {
                            coord.Y = -coord.Y;
                        }
                        texC = coord;
                    }


                    var weights = vertToBoneWeight[(uint)i].Select(w => w.Weight).ToArray();
                    var boneIndices = vertToBoneWeight[(uint)i].Select(w => (byte)w.VertexID).ToArray();

                    var v = new PosNormalTexTanSkinned(pos.ToVector3(), norm.ToVector3(), texC.ToVector2(), tan.ToVector3(), weights.First(), boneIndices);
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
            //_skinnedData.Set(boneHierarchy.ToList(), boneOffsets.ToList(), animations);


            _modelMesh.SetSubsetTable(_subsets);
            _modelMesh.SetVertices(device, _vertices);
            _modelMesh.SetIndices(device, _indices);
        }

        protected internal SceneAnimator Animator { get; set; }






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
        public string ClipName {
            get { return _clipName; }
            set {
                if (Model.Animator.Animations.Any(a => a.Name == value)) {
                    _clipName = value;
                } else {
                    _clipName = "Still";
                }
                Model.Animator.SetAnimation(_clipName);
                TimePos = 0;

            }
        }

        public IEnumerable<string> Clips { get { return Model.Animator.Animations.Select(a => a.Name); } } 
        private Queue<string> _clipQueue = new Queue<string>();

        public Matrix World;
        public List<Matrix> FinalTransforms = new List<Matrix>();
        private string _clipName;
        public bool LoopClips { get; set; }

        public SkinnedModelInstance(string clipName, Matrix transform, SkinnedModel model) {

            World = transform;
            Model = model;

            ClipName = clipName;

        }

        public void Update(float dt) {
            TimePos += dt;


            var d = Model.Animator.Duration;
            if (TimePos > d) {
                if (_clipQueue.Any()) {
                    ClipName = _clipQueue.Dequeue();
                    if (LoopClips) {
                        _clipQueue.Enqueue(ClipName);
                    }
                } else {
                    ClipName = "Still";
                }
            }

            FinalTransforms = Model.Animator.GetTransforms(TimePos);

        }
        public void AddClip(string clip) {
            if (Model.Animator.Animations.Any(a => a.Name == clip)) {
                _clipQueue.Enqueue(clip);
            }
        }
        public void ClearClips() {
            _clipQueue.Clear();
        }
    }
}