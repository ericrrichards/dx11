using System.Drawing;
using System.Runtime.InteropServices;
using SlimDX;

namespace Core {
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

        public const int Stride = 28;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexPN {
        public Vector3 Pos;
        public Vector3 Normal;

        public VertexPN(Vector3 pos, Vector3 n) {
            Pos = pos;
            Normal = n;
        }

        public const int Stride = 24;
    }
}