using System.Drawing;
using System.Runtime.InteropServices;
using SlimDX;

namespace Core {
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex {
        public Vector3 Pos;
        public Color4 Color;

        public Vertex(Vector3 pos, Color color) {
            Pos = pos;
            Color = color;
        }

        public Vertex(Vector3 pos, Color4 color) {
            Pos = pos;
            Color = color;
        }

        public const int Stride = 28;
    }
}