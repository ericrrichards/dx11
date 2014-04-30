using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SlimDX;
using SlimDX.Direct3D9;

namespace HemisphericalAmbient {

    [StructLayout(LayoutKind.Sequential)]
    public struct SDKMeshHeader {
        public uint Version;
        public byte IsBigEndian;
        public UInt64 HeaderSize;
        public UInt64 NonBufferDataSize;
        public UInt64 BufferDataSize;
        public uint NumVertexBuffers;
        public uint NumIndexBuffers;
        public uint NumMeshes;
        public uint NumTotalSubsets;
        public uint NumFrames;
        public uint NumMaterials;
        public UInt64 VertexStreamHeaderOffset;
        public UInt64 IndexStreamHeaderOffset;
        public UInt64 MeshDataOffset;
        public UInt64 SubsetDataOffset;
        public UInt64 FrameDataOffset;
        public UInt64 MaterialDataOffset;

        public override string ToString() {
            var sb = new StringBuilder();
            foreach (var fieldInfo in this.GetType().GetFields()) {
                sb.AppendLine(fieldInfo.Name + ": " + fieldInfo.GetValue(this));
            }
            return sb.ToString();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDKMeshVertexBufferHeader {
        public const int MaxVertexElements = 32;
        public UInt64 NumVertices;
        public UInt64 SizeBytes;
        public UInt64 StrideBytes;
        public List<VertexElement> Decl;
        public UInt64 DataOffset;

        public override string ToString() {
            var sb = new StringBuilder();
            sb.AppendLine("NumVertices: " + NumVertices);
            sb.AppendLine("SizeBytes: " + SizeBytes);
            sb.AppendLine("StrideBytes: " + StrideBytes);
            sb.AppendLine("Decl: ");
            foreach (var elem in Decl) {
                sb.AppendLine("\tVertexElement(Stream: " + elem.Stream + " Offset: " + elem.Offset + " Type: " + elem.Type + " Method: " + elem.Method + " Usage: " + elem.Usage + " UsageIndex: " + elem.UsageIndex + ")");
            }

            sb.AppendLine("DataOffset: " + DataOffset);
            return sb.ToString();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDKMeshIndexBufferHeader {
        public UInt64 NumIndices;
        public UInt64 SizeBytes;
        public uint IndexType;
        public UInt64 DataOffset;
        public override string ToString() {
            var sb = new StringBuilder();
            foreach (var fieldInfo in this.GetType().GetFields()) {
                sb.AppendLine(fieldInfo.Name + ": " + fieldInfo.GetValue(this));
            }
            return sb.ToString();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDKMeshMesh {
        public string Name;
        public byte NumVertexBuffers;
        public List<uint> VertexBuffers;
        public uint IndexBuffer;
        public uint NumSubsets;
        public uint NumFrameInfluences;

        public Vector3 BoundingBoxCenter;
        public Vector3 BoundingBoxExtents;

        public UInt64 SubsetOffset;
        public UInt64 FrameInfluenceOffset;
        public const int MaxMeshName = 100;
        public const int MaxVertexStreams = 16;

        public override string ToString() {
            var sb = new StringBuilder();
            sb.AppendLine("Name: " + Name);
            sb.AppendLine("NumVertexBuffers: " + NumVertexBuffers);
            sb.Append("VertexBuffers: ");
            foreach (var vertexBuffer in VertexBuffers) {
                sb.Append(vertexBuffer + ", ");
            }
            sb.AppendLine();
            sb.AppendLine("IndexBuffer: " + IndexBuffer);
            sb.AppendLine("NumSubsets: " + NumSubsets);
            sb.AppendLine("NumFrameInfluences: " + NumFrameInfluences);
            sb.AppendLine("BoundingBoxCenter: " + BoundingBoxCenter);
            sb.AppendLine("BoundingBoxExtents: " + BoundingBoxExtents);
            sb.AppendLine("SubsetOffset: " + SubsetOffset);
            sb.AppendLine("FrameInfluenceOffset: " + SubsetOffset);

            return sb.ToString();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDKMeshSubset {
        public const int MaxSubsetName = 100;
        public string Name;
        public uint MaterialID;
        public uint PrimitiveType;
        public UInt64 IndexStart;
        public UInt64 IndexCount;
        public UInt64 VertexStart;
        public UInt64 VertexCount;

        public override string ToString() {
            var sb = new StringBuilder();
            foreach (var fieldInfo in this.GetType().GetFields().Where(fi => !fi.IsLiteral)) {
                sb.AppendLine(fieldInfo.Name + ": " + fieldInfo.GetValue(this));
            }
            return sb.ToString();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDKMeshFrame {
        public const int MaxFrameName = 100;
        public string Name;
        public uint Mesh;
        public int ParentFrame;
        public int ChildFrame;
        public int SiblingFrame;
        public Matrix Matrix;
        public int AnimationDataIndex;

        public override string ToString() {
            var sb = new StringBuilder();
            foreach (var fieldInfo in this.GetType().GetFields().Where(fi => !fi.IsLiteral)) {
                sb.AppendLine(fieldInfo.Name + ": " + fieldInfo.GetValue(this));
            }
            return sb.ToString();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDKMeshMaterial {
        public const int MaxMaterialName = 100;
        public const int MaxMaterialPath = 260;
        public const int MaxTextureName = 260;

        public string Name;
        public string MaterialInstancePath;
        public string DiffuseTexture;
        public string NormalTexture;
        public string SpecularTexture;

        public Color4 Diffuse;
        public Color4 Ambient;
        public Color4 Specular;
        public Color4 Emissive;
        public float Power;
        public UInt64 Force64_1;
        public UInt64 Force64_2;
        public UInt64 Force64_3;
        public UInt64 Force64_4;
        public UInt64 Force64_5;
        public UInt64 Force64_6;

        public override string ToString() {
            var sb = new StringBuilder();
            foreach (var fieldInfo in this.GetType().GetFields().Where(fi => !fi.IsLiteral)) {
                sb.AppendLine(fieldInfo.Name + ": " + fieldInfo.GetValue(this));
            }
            return sb.ToString();
        }
    }

    class Program {
        static void Main(string[] args) {
            var filename = "Meshes/bunny.sdkmesh";


            using (var reader = new BinaryReader(new FileStream(filename, FileMode.Open))) {
                var header = ExtractHeader(reader);
                Console.WriteLine(header);

                for (int i = 0; i < header.NumVertexBuffers; i++) {
                    var vbHeader = ExtractVBHeader(reader);

                    Console.WriteLine();
                    Console.WriteLine(vbHeader);
                }
                for (int i = 0; i < header.NumIndexBuffers; i++) {
                    var ibHeader = ExtractIBHeader(reader);

                    Console.WriteLine();
                    Console.WriteLine(ibHeader);
                }
                for (int i = 0; i < header.NumMeshes; i++) {
                    var mesh = ExtractMesh(reader);

                    Console.WriteLine();
                    Console.WriteLine(mesh);
                }
                for (int i = 0; i < header.NumTotalSubsets; i++) {
                    var subset = ExtractSubset(reader);

                    Console.WriteLine();
                    Console.WriteLine(subset);
                }
                for (int i = 0; i < header.NumFrames; i++) {
                    var frame = ExtractFrame(reader);

                    Console.WriteLine();
                    Console.WriteLine(frame);
                }
                for (int i = 0; i < header.NumMaterials; i++) {
                    var mat = ExtractMaterial(reader);

                    Console.WriteLine();
                    Console.WriteLine(mat);
                }


            }
        }

        private static SDKMeshMaterial ExtractMaterial(BinaryReader reader) {
            var mat = new SDKMeshMaterial();
            mat.Name = Encoding.Default.GetString(reader.ReadBytes(SDKMeshMaterial.MaxMaterialName));
            if (mat.Name[0] == '\0') {
                mat.Name = "";
            }
            mat.MaterialInstancePath = Encoding.Default.GetString(reader.ReadBytes(SDKMeshMaterial.MaxMaterialPath));
            mat.DiffuseTexture = Encoding.Default.GetString(reader.ReadBytes(SDKMeshMaterial.MaxMaterialPath));
            mat.NormalTexture = Encoding.Default.GetString(reader.ReadBytes(SDKMeshMaterial.MaxMaterialPath));
            mat.SpecularTexture = Encoding.Default.GetString(reader.ReadBytes(SDKMeshMaterial.MaxMaterialPath));

            mat.Diffuse = new Color4() {
                Red = reader.ReadSingle(),
                Green = reader.ReadSingle(),
                Blue = reader.ReadSingle(),
                Alpha = reader.ReadSingle()
            };
            mat.Ambient = new Color4() {
                Red = reader.ReadSingle(),
                Green = reader.ReadSingle(),
                Blue = reader.ReadSingle(),
                Alpha = reader.ReadSingle()
            };
            mat.Specular = new Color4() {
                Red = reader.ReadSingle(),
                Green = reader.ReadSingle(),
                Blue = reader.ReadSingle(),
                Alpha = reader.ReadSingle()
            };
            mat.Emissive = new Color4() {
                Red = reader.ReadSingle(),
                Green = reader.ReadSingle(),
                Blue = reader.ReadSingle(),
                Alpha = reader.ReadSingle()
            };
            mat.Power = reader.ReadSingle();
            mat.Force64_1 = reader.ReadUInt64();
            mat.Force64_2 = reader.ReadUInt64();
            mat.Force64_3 = reader.ReadUInt64();
            mat.Force64_4 = reader.ReadUInt64();
            mat.Force64_5 = reader.ReadUInt64();
            mat.Force64_6 = reader.ReadUInt64();
            return mat;
        }

        private static SDKMeshFrame ExtractFrame(BinaryReader reader) {
            var frame = new SDKMeshFrame();
            frame.Name = Encoding.Default.GetString(reader.ReadBytes(SDKMeshFrame.MaxFrameName));
            if (frame.Name[0] == '\0') {
                frame.Name = "";
            }
            frame.Mesh = reader.ReadUInt32();
            frame.ParentFrame = reader.ReadInt32();
            frame.ChildFrame = reader.ReadInt32();
            frame.SiblingFrame = reader.ReadInt32();
            frame.Matrix = new Matrix();
            for (int j = 0; j < 4; j++) {
                for (int k = 0; k < 4; k++) {
                    frame.Matrix[k, j] = reader.ReadSingle();
                }
            }
            frame.AnimationDataIndex = reader.ReadInt32();
            return frame;
        }

        private static SDKMeshSubset ExtractSubset(BinaryReader reader) {
            var subset = new SDKMeshSubset();
            subset.Name = Encoding.Default.GetString(reader.ReadBytes(SDKMeshSubset.MaxSubsetName));
            if (subset.Name[0] == '\0') {
                subset.Name = "";
            }
            subset.MaterialID = reader.ReadUInt32();
            subset.PrimitiveType = reader.ReadUInt32();
            reader.ReadUInt32();
            subset.IndexStart = reader.ReadUInt64();
            subset.IndexCount = reader.ReadUInt64();
            subset.VertexStart = reader.ReadUInt64();
            subset.VertexCount = reader.ReadUInt64();
            return subset;
        }

        private static SDKMeshMesh ExtractMesh(BinaryReader reader) {
            var mesh = new SDKMeshMesh();
            mesh.Name = Encoding.Default.GetString(reader.ReadBytes(SDKMeshMesh.MaxMeshName));
            mesh.NumVertexBuffers = reader.ReadByte();
            reader.ReadBytes(3);
            mesh.VertexBuffers = new List<uint>();
            for (int j = 0; j < SDKMeshMesh.MaxVertexStreams; j++) {
                mesh.VertexBuffers.Add(reader.ReadUInt32());
            }
            mesh.IndexBuffer = reader.ReadUInt32();
            mesh.NumSubsets = reader.ReadUInt32();
            mesh.NumFrameInfluences = reader.ReadUInt32();
            mesh.BoundingBoxCenter = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            mesh.BoundingBoxExtents = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            reader.ReadUInt32();
            mesh.SubsetOffset = reader.ReadUInt64();
            mesh.FrameInfluenceOffset = reader.ReadUInt64();
            return mesh;
        }

        private static SDKMeshIndexBufferHeader ExtractIBHeader(BinaryReader reader) {
            var ibHeader = new SDKMeshIndexBufferHeader();
            ibHeader.NumIndices = reader.ReadUInt64();
            ibHeader.SizeBytes = reader.ReadUInt64();
            ibHeader.IndexType = reader.ReadUInt32();
            reader.ReadUInt32();
            ibHeader.DataOffset = reader.ReadUInt64();
            return ibHeader;
        }

        private static SDKMeshVertexBufferHeader ExtractVBHeader(BinaryReader reader) {
            var vbHeader = new SDKMeshVertexBufferHeader();
            vbHeader.NumVertices = reader.ReadUInt64();
            vbHeader.SizeBytes = reader.ReadUInt64();
            vbHeader.StrideBytes = reader.ReadUInt64();
            vbHeader.Decl = new List<VertexElement>();
            var processElem = true;
            for (int j = 0; j < SDKMeshVertexBufferHeader.MaxVertexElements; j++) {
                var stream = reader.ReadUInt16();
                var offset = reader.ReadUInt16();
                var type = reader.ReadByte();
                var method = reader.ReadByte();
                var usage = reader.ReadByte();
                var usageIndex = reader.ReadByte();
                if (stream < 8 && processElem) {
                    var element = new VertexElement((short)stream, (short)offset, (DeclarationType)type, (DeclarationMethod)method, (DeclarationUsage)usage, usageIndex);
                    vbHeader.Decl.Add(element);
                } else {
                    processElem = false;
                }
            }
            vbHeader.DataOffset = reader.ReadUInt64();
            return vbHeader;
        }

        private static SDKMeshHeader ExtractHeader(BinaryReader reader) {
            var header = new SDKMeshHeader();
            header.Version = reader.ReadUInt32();
            header.IsBigEndian = reader.ReadByte();
            reader.ReadBytes(3);
            header.HeaderSize = reader.ReadUInt64();
            header.NonBufferDataSize = reader.ReadUInt64();
            header.BufferDataSize = reader.ReadUInt64();
            header.NumVertexBuffers = reader.ReadUInt32();
            header.NumIndexBuffers = reader.ReadUInt32();
            header.NumMeshes = reader.ReadUInt32();
            header.NumTotalSubsets = reader.ReadUInt32();
            header.NumFrames = reader.ReadUInt32();
            header.NumMaterials = reader.ReadUInt32();
            header.VertexStreamHeaderOffset = reader.ReadUInt64();
            header.IndexStreamHeaderOffset = reader.ReadUInt64();
            header.MeshDataOffset = reader.ReadUInt64();
            header.SubsetDataOffset = reader.ReadUInt64();
            header.FrameDataOffset = reader.ReadUInt64();
            header.MaterialDataOffset = reader.ReadUInt64();
            return header;
        }
    }
}
