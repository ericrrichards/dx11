using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Core.Vertex;
using SlimDX;
using SlimDX.Direct3D9;

namespace Core.Model {
    /// <summary>
    /// Refer to https://dxut.codeplex.com/SourceControl/latest#Optional/SDKmesh.h
    /// and https://dxut.codeplex.com/SourceControl/latest#Optional/SDKmesh.cpp
    /// </summary>
    internal class SdkMesh {
        private SdkMeshHeader _header;
        internal readonly List<SdkMeshVertexBuffer> VertexBuffers = new List<SdkMeshVertexBuffer>();
        internal readonly List<SdkMeshIndexBuffer> IndexBuffers = new List<SdkMeshIndexBuffer>();
        internal readonly List<SdkMeshMesh> Meshes = new List<SdkMeshMesh>();
        internal readonly List<SdkMeshSubset> Subsets = new List<SdkMeshSubset>();
        internal readonly List<SdkMeshFrame> Frames = new List<SdkMeshFrame>();
        internal readonly List<SdkMeshMaterial> Materials = new List<SdkMeshMaterial>();

        public override string ToString() {
            var sb = new StringBuilder();
            sb.AppendLine(_header.ToString());
            foreach (var vertexBuffer in VertexBuffers) {
                sb.AppendLine(vertexBuffer.ToString());
            }
            foreach (var indexBuffer in IndexBuffers) {
                sb.AppendLine(indexBuffer.ToString());
            }
            foreach (var mesh in Meshes) {
                sb.AppendLine(mesh.ToString());
            }
            foreach (var subset in Subsets) {
                sb.AppendLine(subset.ToString());
            }
            foreach (var frame in Frames) {
                sb.AppendLine(frame.ToString());
            }
            foreach (var material in Materials) {
                sb.AppendLine(material.ToString());
            }
            return sb.ToString();
        }

        public SdkMesh(string filename) {
            using (var reader = new BinaryReader(new FileStream(filename, FileMode.Open))) {
                _header = new SdkMeshHeader(reader);
                for (int i = 0; i < _header.NumVertexBuffers; i++) {
                    VertexBuffers.Add(new SdkMeshVertexBuffer(reader));
                }
                for (int i = 0; i < _header.NumIndexBuffers; i++) {
                    IndexBuffers.Add(new SdkMeshIndexBuffer(reader));
                }
                for (int i = 0; i < _header.NumMeshes; i++) {
                    Meshes.Add(new SdkMeshMesh(reader));
                }
                for (int i = 0; i < _header.NumTotalSubsets; i++) {
                    Subsets.Add(new SdkMeshSubset(reader));
                }
                for (int i = 0; i < _header.NumFrames; i++) {
                    Frames.Add(new SdkMeshFrame(reader));
                }
                for (int i = 0; i < _header.NumMaterials; i++) {
                    Materials.Add(new SdkMeshMaterial(reader));
                }
            }
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct SdkMeshHeader {
            public readonly uint Version;
            public readonly byte IsBigEndian;
            public readonly UInt64 HeaderSize;
            public readonly UInt64 NonBufferDataSize;
            public readonly UInt64 BufferDataSize;
            public readonly uint NumVertexBuffers;
            public readonly uint NumIndexBuffers;
            public readonly uint NumMeshes;
            public readonly uint NumTotalSubsets;
            public readonly uint NumFrames;
            public readonly uint NumMaterials;
            public readonly UInt64 VertexStreamHeaderOffset;
            public readonly UInt64 IndexStreamHeaderOffset;
            public readonly UInt64 MeshDataOffset;
            public readonly UInt64 SubsetDataOffset;
            public readonly UInt64 FrameDataOffset;
            public readonly UInt64 MaterialDataOffset;

            public override string ToString() {
                var sb = new StringBuilder();
                foreach (var fieldInfo in GetType().GetFields()) {
                    sb.AppendLine(fieldInfo.Name + ": " + fieldInfo.GetValue(this));
                }
                return sb.ToString();
            }

            public SdkMeshHeader(BinaryReader reader) {
                Version = reader.ReadUInt32();
                IsBigEndian = reader.ReadByte();
                reader.ReadBytes(3); // allow for padding
                HeaderSize = reader.ReadUInt64();
                NonBufferDataSize = reader.ReadUInt64();
                BufferDataSize = reader.ReadUInt64();
                NumVertexBuffers = reader.ReadUInt32();
                NumIndexBuffers = reader.ReadUInt32();
                NumMeshes = reader.ReadUInt32();
                NumTotalSubsets = reader.ReadUInt32();
                NumFrames = reader.ReadUInt32();
                NumMaterials = reader.ReadUInt32();
                VertexStreamHeaderOffset = reader.ReadUInt64();
                IndexStreamHeaderOffset = reader.ReadUInt64();
                MeshDataOffset = reader.ReadUInt64();
                SubsetDataOffset = reader.ReadUInt64();
                FrameDataOffset = reader.ReadUInt64();
                MaterialDataOffset = reader.ReadUInt64();
            }
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct SdkMeshVertexBuffer {
            private const int MaxVertexElements = 32;

            public readonly UInt64 NumVertices;
            public readonly UInt64 SizeBytes;
            public readonly UInt64 StrideBytes;
            public readonly List<VertexElement> Decl;
            public readonly UInt64 DataOffset;


            public readonly List<PosNormalTexTan> Vertices;

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
                sb.AppendLine("Vertices in vertex buffer: " + Vertices.Count);
                return sb.ToString();
            }

            public SdkMeshVertexBuffer(BinaryReader reader) {

                NumVertices = reader.ReadUInt64();
                SizeBytes = reader.ReadUInt64();
                StrideBytes = reader.ReadUInt64();
                Decl = new List<VertexElement>();
                var processElem = true;
                for (int j = 0; j < MaxVertexElements; j++) {
                    var stream = reader.ReadUInt16();
                    var offset = reader.ReadUInt16();
                    var type = reader.ReadByte();
                    var method = reader.ReadByte();
                    var usage = reader.ReadByte();
                    var usageIndex = reader.ReadByte();
                    if (stream < 16 && processElem) {
                        var element = new VertexElement((short)stream, (short)offset, (DeclarationType)type, (DeclarationMethod)method, (DeclarationUsage)usage, usageIndex);
                        Decl.Add(element);
                    } else {
                        processElem = false;
                    }
                }
                DataOffset = reader.ReadUInt64();
                Vertices = new List<PosNormalTexTan>();
                if (SizeBytes > 0) {
                    ReadVertices(reader);
                }
            }

            private void ReadVertices(BinaryReader reader) {
                var curPos = reader.BaseStream.Position;
                reader.BaseStream.Seek((long)DataOffset, SeekOrigin.Begin);
                //var data = reader.ReadBytes((int) vbHeader.SizeBytes);
                for (ulong i = 0; i < NumVertices; i++) {
                    var vertex = new PosNormalTexTan();
                    foreach (var element in Decl) {
                        switch (element.Type) {
                            case DeclarationType.Float3:
                                var v3 = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                                switch (element.Usage) {
                                    case DeclarationUsage.Position:
                                        vertex.Pos = v3;
                                        break;
                                    case DeclarationUsage.Normal:
                                        vertex.Normal = v3;
                                        break;
                                    case DeclarationUsage.Tangent:
                                        vertex.Tan = v3;
                                        break;
                                }
                                //Console.WriteLine("{0} - {1}", element.Usage, v3);
                                break;
                            case DeclarationType.Float2:
                                var v2 = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                                switch (element.Usage) {
                                    case DeclarationUsage.TextureCoordinate:
                                        vertex.Tex = v2;
                                        break;
                                }
                                //Console.WriteLine("{0} - {1}", element.Usage, v2);
                                break;
                        }
                    }
                    Vertices.Add(vertex);
                }
                reader.BaseStream.Position = curPos;
            }
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct SdkMeshIndexBuffer {
            public readonly UInt64 NumIndices;
            public readonly UInt64 SizeBytes;
            public readonly uint IndexType;
            public readonly UInt64 DataOffset;
            public readonly List<int> Indices;
            public override string ToString() {
                var sb = new StringBuilder();
                sb.AppendLine("NumIndices: " + NumIndices);
                sb.AppendLine("SizeBytes: " + SizeBytes);
                sb.AppendLine("IndexType: " + IndexType);
                sb.AppendLine("DataOffset: " + DataOffset);
                sb.AppendLine("Number of indices in buffer: " + Indices.Count);
                return sb.ToString();
            }
            public SdkMeshIndexBuffer(BinaryReader reader) {

                NumIndices = reader.ReadUInt64();
                SizeBytes = reader.ReadUInt64();
                IndexType = reader.ReadUInt32();
                reader.ReadUInt32(); // padding
                DataOffset = reader.ReadUInt64();

                Indices = new List<int>();
                if (SizeBytes > 0) {
                    ReadIndices(reader);
                }
            }

            private void ReadIndices(BinaryReader reader) {
                var curPos = reader.BaseStream.Position;
                reader.BaseStream.Seek((long)DataOffset, SeekOrigin.Begin);
                for (ulong i = 0; i < NumIndices; i++) {
                    int idx;
                    if (IndexType == 0) {
                        idx = reader.ReadUInt16();
                        Indices.Add(idx);
                    } else {
                        idx = reader.ReadInt32();
                        Indices.Add(idx);
                    }
                }
                reader.BaseStream.Position = curPos;
            }
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct SdkMeshMesh {
            public readonly string Name;
            public readonly byte NumVertexBuffers;
            public readonly List<uint> VertexBuffers;
            public readonly uint IndexBuffer;
            public readonly uint NumSubsets;
            public readonly uint NumFrameInfluences; // bones

            public readonly Vector3 BoundingBoxCenter;
            public readonly Vector3 BoundingBoxExtents;

            public readonly UInt64 SubsetOffset;
            public readonly UInt64 FrameInfluenceOffset; // offset to bone data
            public readonly List<int> SubsetData;
            private const int MaxMeshName = 100;
            private const int MaxVertexStreams = 16;

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
                sb.Append("Subsets: ");
                foreach (var i in SubsetData) {
                    sb.Append(i + ", ");
                }
                sb.AppendLine();

                return sb.ToString();
            }
            public SdkMeshMesh(BinaryReader reader) {

                Name = Encoding.Default.GetString(reader.ReadBytes(MaxMeshName));
                NumVertexBuffers = reader.ReadByte();
                reader.ReadBytes(3);
                VertexBuffers = new List<uint>();
                for (int j = 0; j < MaxVertexStreams; j++) {
                    VertexBuffers.Add(reader.ReadUInt32());
                }
                IndexBuffer = reader.ReadUInt32();
                NumSubsets = reader.ReadUInt32();
                NumFrameInfluences = reader.ReadUInt32();
                BoundingBoxCenter = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                BoundingBoxExtents = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                reader.ReadUInt32();
                SubsetOffset = reader.ReadUInt64();
                FrameInfluenceOffset = reader.ReadUInt64();

                SubsetData = new List<int>();
                if (NumSubsets > 0) {
                    ReadSubsets(reader);
                }
                // NOTE: not bothering with bone data now
            }

            private void ReadSubsets(BinaryReader reader) {
                var curPos = reader.BaseStream.Position;
                reader.BaseStream.Seek((long)SubsetOffset, SeekOrigin.Begin);
                for (int i = 0; i < NumSubsets; i++) {
                    var subsetId = reader.ReadInt32();
                    SubsetData.Add(subsetId);
                }

                reader.BaseStream.Position = curPos;
            }
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct SdkMeshSubset {
            private const int MaxSubsetName = 100;
            public readonly string Name;
            public readonly uint MaterialID;
            public readonly uint PrimitiveType;
            public readonly UInt64 IndexStart;
            public readonly UInt64 IndexCount;
            public readonly UInt64 VertexStart;
            public readonly UInt64 VertexCount;

            public override string ToString() {
                var sb = new StringBuilder();
                foreach (var fieldInfo in GetType().GetFields().Where(fi => !fi.IsLiteral)) {
                    sb.AppendLine(fieldInfo.Name + ": " + fieldInfo.GetValue(this));
                }
                return sb.ToString();
            }
            public SdkMeshSubset(BinaryReader reader) {

                Name = Encoding.Default.GetString(reader.ReadBytes(MaxSubsetName));
                if (Name[0] == '\0') {
                    Name = "";
                }
                MaterialID = reader.ReadUInt32();
                PrimitiveType = reader.ReadUInt32();
                reader.ReadUInt32();
                IndexStart = reader.ReadUInt64();
                IndexCount = reader.ReadUInt64();
                VertexStart = reader.ReadUInt64();
                VertexCount = reader.ReadUInt64();

            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SdkMeshFrame {
            private const int MaxFrameName = 100;
            public readonly string Name;
            public readonly uint Mesh;
            public readonly int ParentFrame;
            public readonly int ChildFrame;
            public readonly int SiblingFrame;
            public readonly Matrix Matrix;
            public readonly int AnimationDataIndex;

            public override string ToString() {
                var sb = new StringBuilder();
                foreach (var fieldInfo in GetType().GetFields().Where(fi => !fi.IsLiteral)) {
                    sb.AppendLine(fieldInfo.Name + ": " + fieldInfo.GetValue(this));
                }
                return sb.ToString();
            }
            public SdkMeshFrame(BinaryReader reader) {

                Name = Encoding.Default.GetString(reader.ReadBytes(MaxFrameName));
                if (Name[0] == '\0') {
                    Name = "";
                }
                Mesh = reader.ReadUInt32();
                ParentFrame = reader.ReadInt32();
                ChildFrame = reader.ReadInt32();
                SiblingFrame = reader.ReadInt32();
                Matrix = new Matrix();
                for (int j = 0; j < 4; j++) {
                    for (int k = 0; k < 4; k++) {
                        Matrix[k, j] = reader.ReadSingle();
                    }
                }
                AnimationDataIndex = reader.ReadInt32();

            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SdkMeshMaterial {
            private const int MaxMaterialName = 100;
            private const int MaxMaterialPath = 260;
            private const int MaxTextureName = 260;

            public readonly string Name;
            public readonly string MaterialInstancePath;
            public readonly string DiffuseTexture;
            public readonly string NormalTexture;
            public readonly string SpecularTexture;

            public readonly Color4 Diffuse;
            public readonly Color4 Ambient;
            public readonly Color4 Specular;
            public readonly Color4 Emissive;
            public readonly float Power;

            public override string ToString() {
                var sb = new StringBuilder();
                foreach (var fieldInfo in GetType().GetFields().Where(fi => !fi.IsLiteral)) {
                    sb.AppendLine(fieldInfo.Name + ": " + fieldInfo.GetValue(this));
                }
                return sb.ToString();
            }
            public SdkMeshMaterial(BinaryReader reader) {
                Name = Encoding.Default.GetString(reader.ReadBytes(MaxMaterialName));
                if (Name[0] == '\0') {
                    Name = "";
                }
                MaterialInstancePath = Encoding.Default.GetString(reader.ReadBytes(MaxMaterialPath)).Trim(new[] { ' ', '\0' });
                DiffuseTexture = Encoding.Default.GetString(reader.ReadBytes(MaxTextureName)).Trim(new[] { ' ', '\0' });
                NormalTexture = Encoding.Default.GetString(reader.ReadBytes(MaxTextureName)).Trim(new[] { ' ', '\0' });
                SpecularTexture = Encoding.Default.GetString(reader.ReadBytes(MaxTextureName)).Trim(new[] { ' ', '\0' });

                Diffuse = new Color4 {
                    Red = reader.ReadSingle(),
                    Green = reader.ReadSingle(),
                    Blue = reader.ReadSingle(),
                    Alpha = reader.ReadSingle()
                };
                Ambient = new Color4 {
                    Red = reader.ReadSingle(),
                    Green = reader.ReadSingle(),
                    Blue = reader.ReadSingle(),
                    Alpha = reader.ReadSingle()
                };
                Specular = new Color4 {
                    Red = reader.ReadSingle(),
                    Green = reader.ReadSingle(),
                    Blue = reader.ReadSingle(),
                    Alpha = reader.ReadSingle()
                };
                Emissive = new Color4 {
                    Red = reader.ReadSingle(),
                    Green = reader.ReadSingle(),
                    Blue = reader.ReadSingle(),
                    Alpha = reader.ReadSingle()
                };
                Power = reader.ReadSingle();
                // Padding...
                reader.ReadUInt64();
                reader.ReadUInt64();
                reader.ReadUInt64();
                reader.ReadUInt64();
                reader.ReadUInt64();
                reader.ReadUInt64();
            }
        }
    }
}