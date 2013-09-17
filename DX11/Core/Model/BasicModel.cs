using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assimp;
using Core.Vertex;
using SlimDX;
using SlimDX.Direct3D11;
using Quaternion = SlimDX.Quaternion;

namespace Core.Model {
    public class BasicModel : DisposableClass {
        private bool _disposed;
        private List<MeshGeometry.Subset> _subsets;
        private List<PosNormalTexTan> _vertices;
        private List<short> _indices;
        private MeshGeometry _modelMesh;

        public int SubsetCount { get { return _subsets.Count; } }
        public List<Material> Materials { get; private set; }
        public List<ShaderResourceView> DiffuseMapSRV { get; private set; }
        public MeshGeometry ModelMesh { get { return _modelMesh; } }
        public List<ShaderResourceView> NormalMapSRV { get; private set; }

        public BasicModel(Device device, TextureManager texMgr, string filename, string texturePath ) {
            _subsets = new List<MeshGeometry.Subset>();
            _vertices = new List<PosNormalTexTan>();
            _indices = new List<short>();
            DiffuseMapSRV = new List<ShaderResourceView>();
            NormalMapSRV = new List<ShaderResourceView>();
            Materials = new List<Material>();

            var importer = new AssimpImporter();
            var model = importer.ImportFile(filename, PostProcessSteps.GenerateSmoothNormals|PostProcessSteps.CalculateTangentSpace);
            _modelMesh = new MeshGeometry();
            var verts = new List<PosNormalTexTan>();
            for (int s = 0; s < model.Meshes.Length; s++) {
                
                var mesh = model.Meshes[s];
                var subset = new MeshGeometry.Subset {
                    Id = s,
                    VertexCount = mesh.VertexCount,
                    VertexStart = _vertices.Count,
                    FaceStart = _indices.Count/3,
                    FaceCount = mesh.FaceCount
                };
                _subsets.Add(subset);
                for (int i = 0; i < mesh.VertexCount; i++) {
                    var pos = mesh.HasVertices ? mesh.Vertices[i] : new Vector3D();
                    var norm = mesh.HasNormals ? mesh.Normals[i] : new Vector3D();
                    var texC = mesh.HasTextureCoords(0) ? mesh.GetTextureCoords(0)[i] : new Vector3D();
                    var tan = mesh.HasTangentBasis ? mesh.Tangents[i] : new Vector3D();
                    var v = new PosNormalTexTan(pos.ToVector3(), norm.ToVector3(), texC.ToVector2(), tan.ToVector3());
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
            _modelMesh.SetSubsetTable(_subsets);
            _modelMesh.SetVertices(device, _vertices);
            _modelMesh.SetIndices(device, _indices);
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
    public struct BasicModelInstance {
        public BasicModel Model;
        public Matrix World;
    }

    

    public class SkinnedData {
        private List<int> _boneHierarchy;
        private List<Matrix> _boneOffsets;
        private Dictionary<string, AnimationClip> _animations;
    }

    class AnimationClip {
        private List<BoneAnimation> _boneAnimations;
    }

    class BoneAnimation {
        private List<Keyframe> _keyframes;

        public float StartTime { get { return _keyframes.First().TimePos; } }
        public float EndTime { get { return _keyframes.Last().TimePos; } }
        public Matrix Interpolate(float t) {
            if (t <= _keyframes.First().TimePos) {
                var s = _keyframes.First().Scale;
                var p = _keyframes.First().Translation;
                var q = _keyframes.First().RotationQuat;

                return Matrix.RotationQuaternion(q)*Matrix.Scaling(s)*Matrix.Translation(p);
            } else if (t >= _keyframes.Last().TimePos) {
                var s = _keyframes.Last().Scale;
                var p = _keyframes.Last().Translation;
                var q = _keyframes.Last().RotationQuat;

                return Matrix.RotationQuaternion(q)*Matrix.Scaling(s)*Matrix.Translation(p);
            } else {
                for (int i = 0; i < _keyframes.Count-1; i++) {
                    var k0 = _keyframes[i];
                    var k1 = _keyframes[i + 1];
                    if (t >= k0.TimePos && t <= k1.TimePos) {
                        var lerpPercent = (t - k0.TimePos)/(k1.TimePos - k0.TimePos);
                        var s0 = k0.Scale;
                        var s1 = k1.Scale;

                        var p0 = k0.Translation;
                        var p1 = k1.Translation;

                        var q0 = k0.RotationQuat;
                        var q1 = k1.RotationQuat;
                    }
                }
            }
        }
    }

    class Keyframe {
        public Keyframe() {
            TimePos = 0.0f;
            Translation = new Vector3(0);
            Scale = new Vector3(1);
            RotationQuat = Quaternion.Identity;
        }

        internal float TimePos;
        internal Vector3 Translation;
        internal Vector3 Scale;
        internal Quaternion RotationQuat;
    }
}
