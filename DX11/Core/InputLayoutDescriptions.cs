using SlimDX.DXGI;
using SlimDX.Direct3D11;

namespace Core {
    public static class InputLayoutDescriptions {
        public static readonly InputElement[] PosColor = {
            new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
            new InputElement("COLOR", 0, Format.R32G32B32A32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
        };

        public static readonly InputElement[] Pos = {
            new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0 ), 
        };
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
            new InputElement("NORMAL", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0), 
            new InputElement("TEXCOORD", 0, Format.R32G32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
            new InputElement("WORLD", 0, Format.R32G32B32A32_Float, 0, 1, InputClassification.PerInstanceData, 1 ), 
            new InputElement("WORLD", 1, Format.R32G32B32A32_Float, InputElement.AppendAligned, 1, InputClassification.PerInstanceData, 1 ),
            new InputElement("WORLD", 2, Format.R32G32B32A32_Float, InputElement.AppendAligned, 1, InputClassification.PerInstanceData, 1 ),
            new InputElement("WORLD", 3, Format.R32G32B32A32_Float, InputElement.AppendAligned, 1, InputClassification.PerInstanceData, 1 ),
            new InputElement("COLOR", 0, Format.R32G32B32A32_Float, InputElement.AppendAligned, 1, InputClassification.PerInstanceData, 1 )
        };
        public static readonly InputElement[] PosNormalTexTan = {
            new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
            new InputElement("NORMAL", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0), 
            new InputElement("TEXCOORD", 0, Format.R32G32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
            new InputElement("TANGENT", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData,0 ) 
        };

        public static readonly InputElement[] TerrainCP = {
            new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
            new InputElement("TEXCOORD", 0, Format.R32G32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
            new InputElement("TEXCOORD", 1, Format.R32G32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
        };

        public static readonly InputElement[] PosNormalTexTanSkinned = {
            new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
            new InputElement("NORMAL", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0), 
            new InputElement("TEXCOORD", 0, Format.R32G32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
            new InputElement("TANGENT", 0, Format.R32G32B32A32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData,0 ),
            new InputElement("BLENDWEIGHT", 0, Format.R32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
            new InputElement("BLENDINDICES", 0, Format.R8G8B8A8_UInt, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0), 
        };
        public static readonly InputElement[] InstancedPosNormalTexTan = {
            new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
            new InputElement("NORMAL", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0), 
            new InputElement("TEXCOORD", 0, Format.R32G32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
            new InputElement("TANGENT", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData,0 ),
            new InputElement("WORLD", 0, Format.R32G32B32A32_Float, 0, 1, InputClassification.PerInstanceData, 1 ), 
            new InputElement("WORLD", 1, Format.R32G32B32A32_Float, InputElement.AppendAligned, 1, InputClassification.PerInstanceData, 1 ),
            new InputElement("WORLD", 2, Format.R32G32B32A32_Float, InputElement.AppendAligned, 1, InputClassification.PerInstanceData, 1 ),
            new InputElement("WORLD", 3, Format.R32G32B32A32_Float, InputElement.AppendAligned, 1, InputClassification.PerInstanceData, 1 ),
        };

        public static readonly InputElement[] Particle = {
            new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0), 
            new InputElement("VELOCITY", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0), 
            new InputElement("SIZE", 0, Format.R32G32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
            new InputElement("AGE", 0, Format.R32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
            new InputElement("TYPE", 0, Format.R32_UInt, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
        };
    }
}