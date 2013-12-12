using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assimp;

using Core.Vertex;
using SlimDX;
using SlimDX.Direct3D11;

namespace Core.Model {
    using System.Drawing;

    public class BasicModel : IModel<PosNormalTexTan> {

        public BasicModel() { }

        public BasicModel(Device device, TextureManager texMgr, string filename, string texturePath) {

            var importer = new AssimpImporter();
            if (!importer.IsImportFormatSupported(Path.GetExtension(filename))) {
                throw new ArgumentException("Model format " + Path.GetExtension(filename) + " is not supported!  Cannot load {1}", "filename");
            }
#if DEBUG

            importer.AttachLogStream(new ConsoleLogStream());
            importer.VerboseLoggingEnabled = true;
#endif
            var model = importer.ImportFile(filename, PostProcessSteps.GenerateSmoothNormals | PostProcessSteps.CalculateTangentSpace);


            var min = new Vector3(float.MaxValue);
            var max = new Vector3(float.MinValue);
            var verts = new List<PosNormalTexTan>();
            foreach (var mesh in model.Meshes) {
                var subset = new MeshGeometry.Subset {

                    VertexCount = mesh.VertexCount,
                    VertexStart = Vertices.Count,
                    FaceStart = Indices.Count / 3,
                    FaceCount = mesh.FaceCount
                };
                Subsets.Add(subset);
                // bounding box corners


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

                Vertices.AddRange(verts);

                var indices = mesh.GetIndices().Select(i => (short)(i + (uint)subset.VertexStart)).ToList();
                Indices.AddRange(indices);

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
            BoundingBox = new BoundingBox(min, max);
            ModelMesh.SetSubsetTable(Subsets);
            ModelMesh.SetVertices(device, Vertices);
            ModelMesh.SetIndices(device, Indices);
        }

        protected override void InitFromMeshData(Device device, GeometryGenerator.MeshData mesh) {
            var subset = new MeshGeometry.Subset {
                FaceCount = mesh.Indices.Count / 3,
                FaceStart = 0,
                VertexCount = mesh.Vertices.Count,
                VertexStart = 0
            };
            Subsets.Add(subset);

            var max = new Vector3(float.MinValue);
            var min = new Vector3(float.MaxValue);
            foreach (var vertex in mesh.Vertices) {
                max = Vector3.Maximize(max, vertex.Position);
                min = Vector3.Minimize(min, vertex.Position);
            }

            BoundingBox = new BoundingBox(min, max);

            Vertices.AddRange(mesh.Vertices.Select(v => new PosNormalTexTan(v.Position, v.Normal, v.TexC, v.TangentU)).ToList());
            Indices.AddRange(mesh.Indices.Select(i => (short)i));

            Materials.Add(new Material { Ambient = Color.Gray, Diffuse = Color.White, Specular = new Color4(16, 1, 1, 1) });
            DiffuseMapSRV.Add(null);
            NormalMapSRV.Add(null);

            ModelMesh.SetSubsetTable(Subsets);
            ModelMesh.SetVertices(device, Vertices);
            ModelMesh.SetIndices(device, Indices);
        }

        public override void CreateBox(Device device, float width, float height, float depth) {
            var box = GeometryGenerator.CreateBox(width, height, depth);
            InitFromMeshData(device, box);
        }

        public override void CreateSphere(Device device, float radius, int slices, int stacks) {
            var sphere = GeometryGenerator.CreateSphere(radius, slices, stacks);
            InitFromMeshData(device, sphere);
        }

        public override void CreateCylinder(Device device, float bottomRadius, float topRadius, float height, int sliceCount, int stackCount) {
            var cylinder = GeometryGenerator.CreateCylinder(bottomRadius, topRadius, height, sliceCount, stackCount);
            InitFromMeshData(device, cylinder);
        }

        public override void CreateGrid(Device device, float width, float depth, int xVerts, int zVerts) {
            var grid = GeometryGenerator.CreateGrid(width, depth, xVerts, zVerts);
            InitFromMeshData(device, grid);
        }

        public static BasicModel LoadFromTxtFile(Device device, string filename) {

            var vertices = new List<Basic32>();
            var indices = new List<int>();
            var vcount = 0;
            var tcount = 0;
            using (var reader = new StreamReader(filename)) {


                var input = reader.ReadLine();
                if (input != null)
                    // VertexCount: X
                    vcount = Convert.ToInt32(input.Split(new[] { ':' })[1].Trim());

                input = reader.ReadLine();
                if (input != null)
                    //TriangleCount: X
                    tcount = Convert.ToInt32(input.Split(new[] { ':' })[1].Trim());

                // skip ahead to the vertex data
                do {
                    input = reader.ReadLine();
                } while (input != null && !input.StartsWith("{"));
                // Get the vertices  
                for (int i = 0; i < vcount; i++) {
                    input = reader.ReadLine();
                    if (input != null) {
                        var vals = input.Split(new[] { ' ' });
                        vertices.Add(
                                     new Basic32(
                                         new Vector3(
                                             Convert.ToSingle(vals[0].Trim()),
                                             Convert.ToSingle(vals[1].Trim()),
                                             Convert.ToSingle(vals[2].Trim())),
                                         new Vector3(
                                             Convert.ToSingle(vals[3].Trim()),
                                             Convert.ToSingle(vals[4].Trim()),
                                             Convert.ToSingle(vals[5].Trim())),
                                         new Vector2()
                                         )
                            );
                    }
                }
                // skip ahead to the index data
                do {
                    input = reader.ReadLine();
                } while (input != null && !input.StartsWith("{"));
                // Get the indices

                for (var i = 0; i < tcount; i++) {
                    input = reader.ReadLine();
                    if (input == null) {
                        break;
                    }
                    var m = input.Trim().Split(new[] { ' ' });
                    indices.Add(Convert.ToInt32(m[0].Trim()));
                    indices.Add(Convert.ToInt32(m[1].Trim()));
                    indices.Add(Convert.ToInt32(m[2].Trim()));
                }
            }
            var ret = new BasicModel();

            var subset = new MeshGeometry.Subset {
                FaceCount = indices.Count / 3,
                FaceStart = 0,
                VertexCount = vertices.Count,
                VertexStart = 0
            };
            ret.Subsets.Add(subset);
            var max = new Vector3(float.MinValue);
            var min = new Vector3(float.MaxValue);
            foreach (var vertex in vertices) {
                max = Vector3.Maximize(max, vertex.Position);
                min = Vector3.Minimize(min, vertex.Position);
            }
            ret.BoundingBox = new BoundingBox(min, max);

            ret.Vertices.AddRange(vertices.Select(v => new PosNormalTexTan(v.Position, v.Normal, v.Tex, new Vector3(1, 0, 0))).ToList());
            ret.Indices.AddRange(indices.Select(i => (short)i));

            ret.Materials.Add(new Material { Ambient = Color.Gray, Diffuse = Color.White, Specular = new Color4(16, 1, 1, 1) });
            ret.DiffuseMapSRV.Add(null);
            ret.NormalMapSRV.Add(null);

            ret.ModelMesh.SetSubsetTable(ret.Subsets);
            ret.ModelMesh.SetVertices(device, ret.Vertices);
            ret.ModelMesh.SetIndices(device, ret.Indices);

            return ret;

        }
    }
}
