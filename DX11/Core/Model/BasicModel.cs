using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assimp;
using Core.FX;
using Core.Vertex;
using SlimDX;
using SlimDX.Direct3D11;

namespace Core.Model {
    public class BasicModel : DisposableClass {
        private bool _disposed;
        private readonly List<MeshGeometry.Subset> _subsets;
        private readonly List<PosNormalTexTan> _vertices;
        private readonly List<short> _indices;
        private MeshGeometry _modelMesh;


        public List<Material> Materials { get; private set; }
        public List<ShaderResourceView> DiffuseMapSRV { get; private set; }
        public List<ShaderResourceView> NormalMapSRV { get; private set; }
        public BoundingBox BoundingBox { get; private set; }

        public MeshGeometry ModelMesh { get { return _modelMesh; } }
        public int SubsetCount { get { return _subsets.Count; } }

        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Util.ReleaseCom(ref _modelMesh);
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        public BasicModel(Device device, TextureManager texMgr, string filename, string texturePath) {
            _subsets = new List<MeshGeometry.Subset>();
            _vertices = new List<PosNormalTexTan>();
            _indices = new List<short>();
            DiffuseMapSRV = new List<ShaderResourceView>();
            NormalMapSRV = new List<ShaderResourceView>();
            Materials = new List<Material>();
            _modelMesh = new MeshGeometry();

            var importer = new AssimpImporter();
            if (!importer.IsImportFormatSupported(Path.GetExtension(filename))) {
                throw new ArgumentException("Model format " + Path.GetExtension(filename) + " is not supported!  Cannot load {1}", "filename");
            }
            var model = importer.ImportFile(filename, PostProcessSteps.GenerateSmoothNormals | PostProcessSteps.CalculateTangentSpace);



            var verts = new List<PosNormalTexTan>();
            foreach (var mesh in model.Meshes) {
                var subset = new MeshGeometry.Subset {

                    VertexCount = mesh.VertexCount,
                    VertexStart = _vertices.Count,
                    FaceStart = _indices.Count / 3,
                    FaceCount = mesh.FaceCount
                };
                _subsets.Add(subset);
                // bounding box corners
                var min = new Vector3(float.MaxValue);
                var max = new Vector3(float.MinValue);

                for (var i = 0; i < mesh.VertexCount; i++) {
                    var pos = mesh.HasVertices ? mesh.Vertices[i].ToVector3() : new Vector3();
                    min = Vector3.Minimize(min, pos);
                    max = Vector3.Maximize(max, pos);

                    var norm = mesh.HasNormals ? mesh.Normals[i] : new Vector3D();
                    var texC = mesh.HasTextureCoords(0) ? mesh.GetTextureCoords(0)[i] : new Vector3D();
                    var tan = mesh.HasTangentBasis ? mesh.Tangents[i] : new Vector3D();
                    var v = new PosNormalTexTan(pos, norm.ToVector3(), texC.ToVector2(), tan.ToVector3());
                    verts.Add(v);
                }
                BoundingBox = new BoundingBox(min, max);
                _vertices.AddRange(verts);

                var indices = mesh.GetIndices().Select(i => (short)(i + (uint)subset.VertexStart)).ToList();
                _indices.AddRange(indices);

                var mat = model.Materials[mesh.MaterialIndex];
                var material = mat.ToMaterial();

                Materials.Add(material);

                var diffusePath = mat.GetTexture(TextureType.Diffuse, 0).FilePath;
                if (Path.GetExtension(diffusePath) == ".tga") {
                    // DirectX doesn't like to load tgas, so you will need to convert them to pngs yourself with an image editor
                    diffusePath = diffusePath.Replace(".tga", ".png");
                }
                if (!string.IsNullOrEmpty(diffusePath)) {
                    DiffuseMapSRV.Add(texMgr.CreateTexture(Path.Combine(texturePath, diffusePath)));
                }
                var normalPath = mat.GetTexture(TextureType.Normals, 0).FilePath;
                if (!string.IsNullOrEmpty(normalPath)) {
                    NormalMapSRV.Add(texMgr.CreateTexture(Path.Combine(texturePath, normalPath)));
                } else {
                    var normalExt = Path.GetExtension(diffusePath);
                    normalPath = Path.GetFileNameWithoutExtension(diffusePath) + "_nmap" + normalExt;

                    NormalMapSRV.Add(texMgr.CreateTexture(Path.Combine(texturePath, normalPath)));

                }
            }
            _modelMesh.SetSubsetTable(_subsets);
            _modelMesh.SetVertices(device, _vertices);
            _modelMesh.SetIndices(device, _indices);
        }

    }
    public struct BasicModelInstance {
        public BasicModel Model;
        public Matrix World;
        public BoundingBox BoundingBox {
            get {
                return new BoundingBox(
                    Vector3.TransformCoordinate(Model.BoundingBox.Minimum, World),
                    Vector3.TransformCoordinate(Model.BoundingBox.Maximum, World)
                );
            }
        }
        public void Draw(DeviceContext dc, EffectPass effectPass, Matrix viewProj) {
            var world = World;
            var wit = MathF.InverseTranspose(world);
            var wvp = world * viewProj;

            Effects.NormalMapFX.SetWorld(world);
            Effects.NormalMapFX.SetWorldInvTranspose(wit);
            Effects.NormalMapFX.SetWorldViewProj(wvp);
            Effects.NormalMapFX.SetTexTransform(Matrix.Identity);

            for (int i = 0; i < Model.SubsetCount; i++) {
                Effects.NormalMapFX.SetMaterial(Model.Materials[i]);
                Effects.NormalMapFX.SetDiffuseMap(Model.DiffuseMapSRV[i]);
                Effects.NormalMapFX.SetNormalMap(Model.NormalMapSRV[i]);

                effectPass.Apply(dc);
                Model.ModelMesh.Draw(dc, i);
            }
        }

    }
}
