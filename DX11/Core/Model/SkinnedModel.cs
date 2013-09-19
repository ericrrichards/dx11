using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assimp;
using Core.Camera;
using Core.Model.dx9;
using Core.Vertex;
using SlimDX;
using SlimDX.Direct3D11;
using SlimDX.Direct3D9;
using Device = SlimDX.Direct3D11.Device;

namespace Core.Model {
    public class SkinnedModel : DisposableClass {
        private MeshGeometry _modelMesh;
        //private SkinnedData _skinnedData;
        private readonly List<MeshGeometry.Subset> _subsets;
        private readonly List<PosNormalTexTanSkinned> _vertices;
        private readonly List<short> _indices;
        private bool _disposed;

        public int SubsetCount { get { return _subsets.Count; } }
        public List<Material> Materials { get; private set; }
        public List<ShaderResourceView> DiffuseMapSRV { get; private set; }
        public MeshGeometry ModelMesh { get { return _modelMesh; } }
        public List<ShaderResourceView> NormalMapSRV { get; private set; }
        public SceneAnimator Animator { get; private set; }

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
            var model = importer.ImportFile(filename, PostProcessSteps.GenerateSmoothNormals  |  PostProcessSteps.CalculateTangentSpace | (flipWinding ? PostProcessSteps.FlipWindingOrder : PostProcessSteps.None) );
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
            //_skinnedData.Set(boneHierarchy.ToList(), boneOffsets.ToList(), animations);


            _modelMesh.SetSubsetTable(_subsets);
            _modelMesh.SetVertices(device, _vertices);
            _modelMesh.SetIndices(device, _indices);



        }

        public SkinnedModel(Device device, TextureManager texMgr, string filename, string texturePath) {
            _subsets = new List<MeshGeometry.Subset>();
            _vertices = new List<PosNormalTexTanSkinned>();
            _indices = new List<short>();
            DiffuseMapSRV = new List<ShaderResourceView>();
            NormalMapSRV = new List<ShaderResourceView>();
            Materials = new List<Material>();
            _modelMesh = new MeshGeometry();
            
            Animator = new SceneAnimator();

            var smesh = new SkinnedMesh(filename);
            
            var mesh = smesh.MeshContainer.MeshData.Mesh;
           // mesh.ComputeTangentFrame(TangentOptions.GenerateInPlace);

            var attTable = mesh.GetAttributeTable();
            foreach (var attributeRange in attTable) {
                var s = new MeshGeometry.Subset() {
                    FaceCount = attributeRange.FaceCount,
                    FaceStart = attributeRange.FaceStart,
                    Id = attributeRange.AttribId,
                    VertexCount = attributeRange.VertexCount,
                    VertexStart = attributeRange.VertexStart
                };
                _subsets.Add(s);
            }
            _modelMesh.SetSubsetTable(_subsets);

            var verts = new List<PosNormalTexTanSkinned>();
            var vbs = mesh.LockVertexBuffer(LockFlags.None);
            var dec = mesh.GetDeclaration();

            var pos = vbs.Read<Vector3>();
            var normal = vbs.Read<Vector3>();
            var tex = vbs.Read<Vector2>();
            var weight = vbs.Read<float>();
            var b0 = vbs.Read<byte>();
            var b1 = vbs.Read<byte>();
            var b2 = vbs.Read<byte>();
            var b3 = vbs.Read<byte>();

            mesh.UnlockVertexBuffer();


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
        private int frame = 0;
        public string ClipName;
        public Matrix World;
        public List<Matrix> FinalTransforms = new List<Matrix>();
        public void Update(float dt) {
            TimePos += dt;
            /*FinalTransforms = Model.SkinnedData.GetFinalTransforms(ClipName, TimePos);
            if (TimePos > Model.SkinnedData.GetClipEndTime(ClipName)) {
                TimePos = 0.0f;
            }
             */
            Model.Animator.SetAnimation(ClipName);
            var oldTransforms = new List<Matrix>(FinalTransforms);
            FinalTransforms = Model.Animator.GetTransforms(TimePos);
            if (oldTransforms.Any() && oldTransforms[0] == FinalTransforms[0]) {
                Console.WriteLine("transform has not changed");
            }
        }
        public void DrawSkeleton(DeviceContext dc, CameraBase camera) {
            Model.Animator.RenderSkeleton(dc, camera, null, null, Matrix.Identity);
        }

        public void NextFrame() {
            Model.Animator.SetAnimation(ClipName);
            frame++;
            FinalTransforms = Model.Animator.GetTransforms(frame);

        }
    }
}