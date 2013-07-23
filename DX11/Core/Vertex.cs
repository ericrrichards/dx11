using System.Drawing;
using System.Runtime.InteropServices;
using SlimDX;

namespace Core {
    using SlimDX.DXGI;
    using SlimDX.Direct3D11;

    using Device = SlimDX.Direct3D11.Device;

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
        public Vector3 Position;
        public Vector3 Normal;

        public VertexPN(Vector3 position, Vector3 normal) {
            Position = position;
            Normal = normal;
        }

        public const int Stride = 24;
    }

    public class InputLayoutDescriptions {
        public static readonly InputElement[] PosNormal = new[] {
            new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
            new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0, InputClassification.PerVertexData, 0), 
        };
    }
    public class InputLayouts {
        public static void InitAll(Device device) {
            var passDesc = Effects.BasicFX.Light1Tech.GetPassByIndex(0).Description;
            PosNormal = new InputLayout(device, passDesc.Signature, InputLayoutDescriptions.PosNormal);

        }
        public static void DestroyAll() {
            Util.ReleaseCom(PosNormal);
        }

        public static InputLayout PosNormal;
    }
}