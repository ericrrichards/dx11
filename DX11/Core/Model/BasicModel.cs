using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assimp;
using SlimDX;
using SlimDX.Direct3D11;

namespace Core.Model {
    public class BasicModel : DisposableClass {
        private bool _disposed;
        private List<ShaderResourceView> _normalMapSRV; 
        private List<MeshGeometry.Subset> _subsets;
        private List<PosNormalTexTan> _vertices;
        private List<short> _indices;
        private MeshGeometry _modelMesh;

        public int SubsetCount { get { return _subsets.Count; } }
        public List<Material> Materials { get; private set; }
        public List<ShaderResourceView> DiffuseMapSRV { get; private set; }
        public MeshGeometry ModelMesh { get { return _modelMesh; } }

        public BasicModel(Device device, TextureManager texMgr, string filename, string texturePath ) {
            _subsets = new List<MeshGeometry.Subset>();
            _vertices = new List<PosNormalTexTan>();
            _indices = new List<short>();
            DiffuseMapSRV = new List<ShaderResourceView>();
            _normalMapSRV = new List<ShaderResourceView>();
            Materials = new List<Material>();

            var importer = new AssimpImporter();
            var model = importer.ImportFile(filename, PostProcessSteps.GenerateNormals);
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
                    _normalMapSRV.Add(texMgr.CreateTexture(Path.Combine(texturePath, normalPath)));
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
}
