using System.Drawing;
using System.Runtime.InteropServices;
using SlimDX;

namespace Core {
    using System;

    using Core.FX;

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

        public static readonly int Stride = Marshal.SizeOf(typeof(VertexPC));
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

    [StructLayout(LayoutKind.Sequential)]
    public struct TreePointSprite {
        public Vector3 Pos;
        public Vector2 Size;

        public static readonly int Stride = Marshal.SizeOf(typeof(TreePointSprite));
    }

    public static class InputLayoutDescriptions {
        public static readonly InputElement[] PosNormal = {
            new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
            new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0, InputClassification.PerVertexData, 0)
        };
        public static readonly InputElement[] Basic32 = {
            new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
            new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0, InputClassification.PerVertexData, 0), 
            new InputElement("TEXCOORD", 0, Format.R32G32_Float, 24, 0, InputClassification.PerVertexData, 0)
        };
        public static readonly InputElement[] TreePointSprite = {
            new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0 ),
            new InputElement("SIZE", 0, Format.R32G32_Float, 12,0,InputClassification.PerVertexData, 0) 
        };
        public static readonly InputElement[] InstancedBasic32 = {
            new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
            new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0, InputClassification.PerVertexData, 0), 
            new InputElement("TEXCOORD", 0, Format.R32G32_Float, 24, 0, InputClassification.PerVertexData, 0),
            new InputElement("WORLD", 0, Format.R32G32B32A32_Float, 0, 1, InputClassification.PerInstanceData, 1 ), 
            new InputElement("WORLD", 1, Format.R32G32B32A32_Float, 16, 1, InputClassification.PerInstanceData, 1 ),
            new InputElement("WORLD", 2, Format.R32G32B32A32_Float, 32, 1, InputClassification.PerInstanceData, 1 ),
            new InputElement("WORLD", 3, Format.R32G32B32A32_Float, 48, 1, InputClassification.PerInstanceData, 1 ),
            new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 64, 1, InputClassification.PerInstanceData, 1 )
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
            try {
                passDesc = Effects.TreeSpriteFX.Light3Tech.GetPassByIndex(0).Description;
                TreePointSprite = new InputLayout(device, passDesc.Signature, InputLayoutDescriptions.TreePointSprite);
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
                TreePointSprite = null;
            }
            
        }
        public static void DestroyAll() {
            Util.ReleaseCom(ref PosNormal);
            Util.ReleaseCom(ref Basic32);
            Util.ReleaseCom(ref TreePointSprite);
            Util.ReleaseCom(ref InstancedBasic32);
        }

        public static InputLayout PosNormal;
        public static InputLayout Basic32;
        public static InputLayout TreePointSprite;
        public static InputLayout InstancedBasic32;
    }
}