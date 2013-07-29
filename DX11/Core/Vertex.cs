using System.Drawing;
using System.Runtime.InteropServices;
using SlimDX;

namespace Core {
    using System;

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

        public static readonly int Stride = Marshal.SizeOf(typeof(VertexPN));
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

        public static readonly int Stride = Marshal.SizeOf(typeof(Basic32));
    }

    public static class InputLayoutDescriptions {
        public static readonly InputElement[] PosNormal = {
        new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
        new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0, InputClassification.PerVertexData, 0), 
    };
        public static readonly InputElement[] Basic32 = {
            new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
            new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0, InputClassification.PerVertexData, 0), 
        new InputElement("TEXCOORD", 0, Format.R32G32_Float, 24, 0, InputClassification.PerVertexData, 0), 
        };
    }
    public static class InputLayouts {
        public static void InitAll(Device device) {
            var passDesc = Effects.BasicFX.Light1Tech.GetPassByIndex(0).Description;
            try {
                PosNormal = new InputLayout(device, passDesc.Signature, InputLayoutDescriptions.PosNormal);
            } catch (Direct3D11Exception dex) {
                Console.WriteLine(dex.Message);
                PosNormal = null;
            }
            try {
                Basic32 = new InputLayout(device, passDesc.Signature, InputLayoutDescriptions.Basic32);
            } catch (Direct3D11Exception dex) {
                Console.WriteLine(dex.Message);
                Basic32 = null;
            }
        }
        public static void DestroyAll() {
            Util.ReleaseCom(ref PosNormal);
            Util.ReleaseCom(ref Basic32);
        }

        public static InputLayout PosNormal;
        public static InputLayout Basic32;
    }
}