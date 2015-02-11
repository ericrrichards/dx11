using System;
using System.Collections.Generic;
using System.Globalization;
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

        public BasicModel(Device device, TextureManager texMgr, string filename, string texturePath, bool flipUv = false) {

            var importer = new AssimpContext();
            if (!importer.IsImportFormatSupported(Path.GetExtension(filename))) {
                throw new ArgumentException("Model format " + Path.GetExtension(filename) + " is not supported!  Cannot load {1}", "filename");
            }
#if DEBUG
            var logStream = new ConsoleLogStream();
            logStream.Attach();
            //importer.  .AttachLogStream(new ConsoleLogStream());
            //importer.VerboseLoggingEnabled = true;
#endif
            var postProcessFlags = PostProcessSteps.GenerateSmoothNormals | PostProcessSteps.CalculateTangentSpace;
            if (flipUv) {
                postProcessFlags |= PostProcessSteps.FlipUVs;
            }
            var model = importer.ImportFile(filename, postProcessFlags);


            var min = new Vector3(float.MaxValue);
            var max = new Vector3(float.MinValue);

            foreach (var mesh in model.Meshes) {
                var verts = new List<PosNormalTexTan>();
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
                    var texC = mesh.HasTextureCoords(0) ? mesh.TextureCoordinateChannels[0][i] : new Vector3D();
                    var tan = mesh.HasTangentBasis ? mesh.Tangents[i] : new Vector3D();
                    var v = new PosNormalTexTan(pos, norm.ToVector3(), texC.ToVector2(), tan.ToVector3());
                    verts.Add(v);
                }

                Vertices.AddRange(verts);

                var indices = mesh.GetIndices().Select(i => ((int)i + subset.VertexStart)).ToList();
                Indices.AddRange(indices);

                var mat = model.Materials[mesh.MaterialIndex];
                var material = mat.ToMaterial();

                Materials.Add(material);
                TextureSlot diffuseSlot;
                mat.GetMaterialTexture(TextureType.Diffuse, 0, out diffuseSlot);
                var diffusePath = diffuseSlot.FilePath;
                if (Path.GetExtension(diffusePath) == ".tga") {
                    // DirectX doesn't like to load tgas, so you will need to convert them to pngs yourself with an image editor
                    diffusePath = diffusePath.Replace(".tga", ".png");
                }
                if (!string.IsNullOrEmpty(diffusePath)) {
                    DiffuseMapSRV.Add(texMgr.CreateTexture(Path.Combine(texturePath, diffusePath)));
                }
                TextureSlot normalSlot;
                mat.GetMaterialTexture(TextureType.Normals, 0, out normalSlot);
                var normalPath = normalSlot.FilePath;
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

            Vertices=mesh.Vertices.Select(v => new PosNormalTexTan(v.Position, v.Normal, v.TexC, v.TangentU)).ToList();
            Indices=mesh.Indices.Select(i => i).ToList();

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

        public override void CreateGeosphere(Device device, float radius, GeometryGenerator.SubdivisionCount numSubdivisions) {
            var geosphere = GeometryGenerator.CreateGeosphere(radius, numSubdivisions);
            InitFromMeshData(device, geosphere);
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
                                             Convert.ToSingle(vals[0].Trim(), CultureInfo.InvariantCulture),
                                             Convert.ToSingle(vals[1].Trim(), CultureInfo.InvariantCulture),
                                             Convert.ToSingle(vals[2].Trim(), CultureInfo.InvariantCulture)),
                                         new Vector3(
                                             Convert.ToSingle(vals[3].Trim(), CultureInfo.InvariantCulture),
                                             Convert.ToSingle(vals[4].Trim(), CultureInfo.InvariantCulture),
                                             Convert.ToSingle(vals[5].Trim(), CultureInfo.InvariantCulture)),
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
            ret.Indices.AddRange(indices.Select(i => i));

            ret.Materials.Add(new Material { Ambient = Color.Gray, Diffuse = Color.White, Specular = new Color4(16, 1, 1, 1) });
            ret.DiffuseMapSRV.Add(null);
            ret.NormalMapSRV.Add(null);

            ret.ModelMesh.SetSubsetTable(ret.Subsets);
            ret.ModelMesh.SetVertices(device, ret.Vertices);
            ret.ModelMesh.SetIndices(device, ret.Indices);

            return ret;

        }

        public static BasicModel LoadSdkMesh(Device device, TextureManager texMgr, string filename, string texturePath) {
            // NOTE: this assumes that the model file only contains a single mesh
            var sdkMesh = new SdkMesh(filename);
            var ret = new BasicModel();

            var faceStart = 0;
            var vertexStart = 0;
            foreach (var sdkMeshSubset in sdkMesh.Subsets) {
                var subset = new MeshGeometry.Subset {
                    FaceCount = (int)(sdkMeshSubset.IndexCount / 3),
                    FaceStart = faceStart,
                    VertexCount = (int)sdkMeshSubset.VertexCount,
                    VertexStart = vertexStart
                };
                // fixup any subset indices that assume that all vertices and indices are not in the same buffers
                faceStart = subset.FaceStart + subset.FaceCount;
                vertexStart = subset.VertexStart + subset.VertexCount;
                ret.Subsets.Add(subset);
            }
            var max = new Vector3(float.MinValue);
            var min = new Vector3(float.MaxValue);
            foreach (var vb in sdkMesh.VertexBuffers) {
                foreach (var vertex in vb.Vertices) {
                    max = Vector3.Maximize(max, vertex.Pos);
                    min = Vector3.Minimize(min, vertex.Pos);
                    ret.Vertices.Add(vertex);
                }
            }
            ret.BoundingBox = new BoundingBox(min, max);

            foreach (var ib in sdkMesh.IndexBuffers) {
                ret.Indices.AddRange(ib.Indices.Select(i => i));
            }
            foreach (var sdkMeshMaterial in sdkMesh.Materials) {
                var material = new Material {
                    Ambient = sdkMeshMaterial.Ambient,
                    Diffuse = sdkMeshMaterial.Diffuse,
                    Reflect = Color.Black,
                    Specular = sdkMeshMaterial.Specular
                };
                material.Specular.Alpha = sdkMeshMaterial.Power;
                ret.Materials.Add(material);
                if (!string.IsNullOrEmpty(sdkMeshMaterial.DiffuseTexture)) {
                    ret.DiffuseMapSRV.Add(texMgr.CreateTexture(Path.Combine(texturePath, sdkMeshMaterial.DiffuseTexture)));
                } else {
                    ret.DiffuseMapSRV.Add(texMgr["default"]);
                }
                if (!string.IsNullOrEmpty(sdkMeshMaterial.NormalTexture)) {
                    ret.NormalMapSRV.Add(texMgr.CreateTexture(Path.Combine(texturePath, sdkMeshMaterial.NormalTexture)));
                } else {
                    ret.NormalMapSRV.Add(texMgr["defaultNorm"]);
                }
            }
            ret.ModelMesh.SetSubsetTable(ret.Subsets);
            ret.ModelMesh.SetVertices(device, ret.Vertices);
            ret.ModelMesh.SetIndices(device, ret.Indices);

            return ret;
        }
    }
}
