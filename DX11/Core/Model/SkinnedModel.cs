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
        private readonly List<PosNormalTexSkinned> _vertices;
        private readonly List<short> _indices;
        private bool _disposed;

        public int SubsetCount { get { return _subsets.Count; } }
        public List<Material> Materials { get; private set; }
        public List<ShaderResourceView> DiffuseMapSRV { get; private set; }
        public MeshGeometry ModelMesh { get { return _modelMesh; } }
        public List<ShaderResourceView> NormalMapSRV { get; private set; }

        private readonly AnimationController _animationController;
        private readonly BoneCombination[] _boneCombinations;
        private readonly int _paletteEntries;
        private readonly FrameEx[] _boneMatricesLookup;
        private readonly Matrix[] _boneOffsets;
        private readonly Frame _root;
        internal readonly Dictionary<string, int> Animations;

        public SkinnedModel(Device device, TextureManager texMgr, string filename, string texturePath) {
            _subsets = new List<MeshGeometry.Subset>();
            _vertices = new List<PosNormalTexSkinned>();
            _indices = new List<short>();
            DiffuseMapSRV = new List<ShaderResourceView>();
            NormalMapSRV = new List<ShaderResourceView>();
            Materials = new List<Material>();
            _modelMesh = new MeshGeometry();
            
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

            var vbs = mesh.LockVertexBuffer(LockFlags.None);
            var dec = mesh.GetDeclaration();
            while (vbs.Position < vbs.Length) {
                var pos = vbs.Read<Vector3>();
                var weight = vbs.Read<float>();
                var b0 = vbs.Read<byte>();
                var b1 = vbs.Read<byte>();
                var b2 = vbs.Read<byte>();
                var b3 = vbs.Read<byte>();
                var normal = vbs.Read<Vector3>();
                var tex = vbs.Read<Vector2>();
                _vertices.Add(new PosNormalTexSkinned(pos, normal, tex,  weight, new []{b0,b1, b2, b3} ));
            }
            
            mesh.UnlockVertexBuffer();
            _modelMesh.SetVertices(device, _vertices);
            var ibs = mesh.LockIndexBuffer(LockFlags.None);
            while (ibs.Position < ibs.Length) {
                var i = ibs.Read<short>();
                _indices.Add(i);
            }

            mesh.UnlockIndexBuffer();
            _modelMesh.SetIndices(device, _indices);

            foreach (var mat in smesh.MeshContainer.GetMaterials()) {
                var m = new Material();
                m.Ambient = mat.MaterialD3D.Ambient;
                m.Diffuse = mat.MaterialD3D.Diffuse;
                m.Specular = mat.MaterialD3D.Specular;
                m.Reflect = m.Specular;
                m.Specular.Alpha = mat.MaterialD3D.Power;

                Materials.Add(m);
                var diffusePath = mat.TextureFileName;
                DiffuseMapSRV.Add(texMgr.CreateTexture(Path.Combine(texturePath, diffusePath)));
            }
            _animationController = smesh.AnimationController;

            _boneCombinations = smesh.MeshContainer.BoneCombinations;
            _paletteEntries = smesh.MeshContainer.PaletteEntries;
            _boneOffsets = smesh.MeshContainer.BoneOffsets;
            _boneMatricesLookup = smesh.MeshContainer.BoneMatricesLookup;
            Animations = smesh.Animations;
            _root = smesh.Root;
        }
        public void Update(float dt, AnimationController anim = null) {
            
            if (anim == null) {
                _animationController.AdvanceTime(dt, null);
            } else {
                anim.AdvanceTime(dt, null);
            }

            UpdateFrameMatrices(_root, Matrix.Identity);
        }
        private void UpdateFrameMatrices(Frame frame, Matrix matrix) {
            var frameEx = frame as FrameEx;
            if (frameEx != null) {
                frameEx.ToRoot = frameEx.TransformationMatrix * matrix;

                if (frame.Sibling != null) {
                    UpdateFrameMatrices(frame.Sibling, matrix);
                }
                if (frame.FirstChild != null) {
                    UpdateFrameMatrices(frameEx.FirstChild, frameEx.ToRoot);
                }
            }
        }

        public Matrix[] GetBoneMatrices() {
            var boneMatrices = new Matrix[_paletteEntries];
            var combinations = _boneCombinations;

            for (int i = 0; i < combinations.Length; i++) {
                for (int p = 0; p < _paletteEntries; p++) {
                    var index = combinations[i].BoneIds[p];
                    if (index != -1) {
                        boneMatrices[p] = _boneOffsets[index] * _boneMatricesLookup[index].ToRoot;
                    }
                }
            }
            return boneMatrices;
        }
        public void SetAnimation(string name) {
            if (Animations.ContainsKey(name)) {
                _animationController.SetTrackAnimationSet(0, _animationController.GetAnimationSet<AnimationSet>(Animations[name]));
            }
        }
        public AnimationController GetAnimationControllerClone() {
            return _animationController.Clone(_animationController.MaxAnimationOutputs, _animationController.MaxAnimationSets, _animationController.MaxTracks, _animationController.MaxEvents);
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
        public string ClipName { get { return _clipName; } set {
            if (_clipName != value) {
                _clipName = value;
                _animationController.SetTrackAnimationSet(0, _animationController.GetAnimationSet<AnimationSet>(Animations[_clipName]));
            }
        } }
        public Matrix World;
        public List<Matrix> FinalTransforms = new List<Matrix>();
        private AnimationController _animationController;
        private Dictionary<string, int> Animations;
        private string _clipName;

        public SkinnedModelInstance(SkinnedModel model) {
            Model = model;
            _animationController = model.GetAnimationControllerClone();
            Animations = model.Animations;
        }
        public void Update(float dt) {
            Model.Update(dt, _animationController);
            FinalTransforms = Model.GetBoneMatrices().ToList();
        }
    }
}