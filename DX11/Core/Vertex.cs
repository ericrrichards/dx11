using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using SlimDX;
using SlimDX.D3DCompiler;

namespace Core {
    namespace Vertex {


        [StructLayout(LayoutKind.Sequential)]
        public struct VertexPC {
            public Vector3 Pos;
            public Color4 Color;

            public VertexPC(Vector3 pos, Color color) {
                Pos = pos;
                Color = color;
            }

            public VertexPC(Vector3 pos, Color4 color) {
                Pos = pos;
                Color = color;
            }

            public static readonly int Stride = Marshal.SizeOf(typeof (VertexPC));
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct VertexPN {
            public Vector3 Position;
            public Vector3 Normal;

            public VertexPN(Vector3 position, Vector3 normal) {
                Position = position;
                Normal = normal;
            }

            public static readonly int Stride = Marshal.SizeOf(typeof (VertexPN));
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Basic32 {
            public Vector3 Position;
            public Vector3 Normal;
            public Vector2 Tex;

            public Basic32(Vector3 position, Vector3 normal, Vector2 texC) {
                Position = position;
                Normal = normal;
                Tex = texC;
            }

            public static readonly int Stride = Marshal.SizeOf(typeof (Basic32));
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TreePointSprite {
            public Vector3 Pos;
            public Vector2 Size;

            public static readonly int Stride = Marshal.SizeOf(typeof (TreePointSprite));
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PosNormalTexTan {
            public Vector3 Pos;
            public Vector3 Normal;
            public Vector2 Tex;
            public Vector3 Tan;
            public static readonly int Stride = Marshal.SizeOf(typeof (PosNormalTexTan));

            public PosNormalTexTan(Vector3 position, Vector3 normal, Vector2 texC, Vector3 tangentU) {
                Pos = position;
                Normal = normal;
                Tex = texC;
                Tan = tangentU;
            }
        }

        public struct TerrainCP {
            public Vector3 Pos;
            public Vector2 Tex;
            public Vector2 BoundsY;

            public TerrainCP(Vector3 pos, Vector2 tex, Vector2 boundsY) {
                Pos = pos;
                Tex = tex;
                BoundsY = boundsY;
            }

            public static readonly int Stride = Marshal.SizeOf(typeof(TerrainCP));
        }
        public struct PosNormalTexTanSkinned {
            public Vector3 Pos;
            public Vector3 Normal;
            public Vector2 Tex;
            public Vector4 Tan;
            public float Weight;
            public BonePalette BoneIndices;

            public static readonly int Stride = Marshal.SizeOf(typeof(PosNormalTexTanSkinned));

            public PosNormalTexTanSkinned(Vector3 pos, Vector3 norm, Vector2 uv, Vector3 tan, float weight, byte[] boneIndices) {
                Pos = pos;
                Normal = norm;
                Tex = uv;
                Tan = new Vector4(tan, 0);
                Weight = weight;
                BoneIndices = new BonePalette();
                for (int index = 0; index < boneIndices.Length; index++) {
                    switch (index) {
                        case 0:
                            BoneIndices.B0 = boneIndices[index];
                            break;
                        case 1:
                            BoneIndices.B1 = boneIndices[index];
                            break;
                        case 2:
                            BoneIndices.B2 = boneIndices[index];
                            break;
                        case 3:
                            BoneIndices.B3 = boneIndices[index];
                            break;
                    }
                }
                
            }
        }
        public struct BonePalette {
            public byte B0, B1, B2, B3;
        }

        public struct Particle {
            public Vector3 InitialPos;
            public Vector3 InitialVel;
            public Vector2 Size;
            public float Age;
            public uint Type;

            public static readonly int Stride = Marshal.SizeOf(typeof (Particle));
        }
    }
}
