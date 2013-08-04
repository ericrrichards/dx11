using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX.Direct3D11;

using Debug = System.Diagnostics.Debug;

namespace Core {
    public enum RenderOptions {
        Lighting,
        Textures,
        TexturesAndFog
    }

    public static class RenderStates {
        public static void InitAll(Device device) {
            Debug.Assert(device != null);
            
            var wfDesc = new RasterizerStateDescription {
                FillMode = FillMode.Wireframe,
                CullMode = CullMode.Back,
                IsFrontCounterclockwise = false,
                IsDepthClipEnabled = true
            };
            WireframeRS = RasterizerState.FromDescription(device, wfDesc);

            var noCullDesc = new RasterizerStateDescription {
                FillMode = FillMode.Solid,
                CullMode = CullMode.None,
                IsFrontCounterclockwise = false,
                IsDepthClipEnabled = true
            };
            NoCullRS = RasterizerState.FromDescription(device, noCullDesc);

            var cullClockwiseDesc = new RasterizerStateDescription {
                FillMode = FillMode.Solid,
                CullMode = CullMode.Back,
                IsFrontCounterclockwise = true,
                IsDepthClipEnabled = true
            };
            CullClockwiseRS = RasterizerState.FromDescription(device, cullClockwiseDesc);

            var atcDesc = new BlendStateDescription {
                AlphaToCoverageEnable = true,
                IndependentBlendEnable = false,
            };
            atcDesc.RenderTargets[0].BlendEnable = false;
            atcDesc.RenderTargets[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
            AlphaToCoverageBS = BlendState.FromDescription(device, atcDesc);

            var transDesc = new BlendStateDescription {
                AlphaToCoverageEnable = false,
                IndependentBlendEnable = false
            };
            transDesc.RenderTargets[0].BlendEnable = true;
            transDesc.RenderTargets[0].SourceBlend = BlendOption.SourceAlpha;
            transDesc.RenderTargets[0].DestinationBlend = BlendOption.InverseSourceAlpha;
            transDesc.RenderTargets[0].BlendOperation = BlendOperation.Add;
            transDesc.RenderTargets[0].SourceBlendAlpha = BlendOption.One;
            transDesc.RenderTargets[0].DestinationBlendAlpha = BlendOption.Zero;
            transDesc.RenderTargets[0].BlendOperationAlpha = BlendOperation.Add;
            transDesc.RenderTargets[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;

            TransparentBS = BlendState.FromDescription(device, transDesc);

            var noRenderTargetWritesDesc = new BlendStateDescription {
                AlphaToCoverageEnable = false,
                IndependentBlendEnable = false
            };
            noRenderTargetWritesDesc.RenderTargets[0].BlendEnable = false;
            noRenderTargetWritesDesc.RenderTargets[0].SourceBlend = BlendOption.One;
            noRenderTargetWritesDesc.RenderTargets[0].DestinationBlend = BlendOption.Zero;
            noRenderTargetWritesDesc.RenderTargets[0].BlendOperation = BlendOperation.Add;
            noRenderTargetWritesDesc.RenderTargets[0].SourceBlendAlpha = BlendOption.One;
            noRenderTargetWritesDesc.RenderTargets[0].DestinationBlendAlpha = BlendOption.Zero;
            noRenderTargetWritesDesc.RenderTargets[0].BlendOperationAlpha = BlendOperation.Add;
            noRenderTargetWritesDesc.RenderTargets[0].RenderTargetWriteMask = ColorWriteMaskFlags.None;

            NoRenderTargetWritesBS = BlendState.FromDescription(device, noRenderTargetWritesDesc);

            var mirrorDesc = new DepthStencilStateDescription {
                IsDepthEnabled = true,
                DepthWriteMask = DepthWriteMask.Zero,
                DepthComparison = Comparison.Less,
                IsStencilEnabled = true,
                StencilReadMask = 0xff,
                StencilWriteMask = 0xff, FrontFace = new DepthStencilOperationDescription {
                    FailOperation = StencilOperation.Keep,
                    DepthFailOperation = StencilOperation.Keep,
                    PassOperation = StencilOperation.Replace,
                    Comparison = Comparison.Always
                },
                BackFace = new DepthStencilOperationDescription {
                    FailOperation = StencilOperation.Keep,
                    DepthFailOperation = StencilOperation.Keep,
                    PassOperation = StencilOperation.Replace,
                    Comparison = Comparison.Always
                }
            };

            MarkMirrorDSS = DepthStencilState.FromDescription(device, mirrorDesc);

            var drawReflectionDesc = new DepthStencilStateDescription {
                IsDepthEnabled = true,
                DepthWriteMask = DepthWriteMask.All,
                DepthComparison = Comparison.Less,
                IsStencilEnabled = true,
                StencilReadMask = 0xff,
                StencilWriteMask = 0xff,
                FrontFace = new DepthStencilOperationDescription {
                    FailOperation = StencilOperation.Keep,
                    DepthFailOperation = StencilOperation.Keep,
                    PassOperation = StencilOperation.Keep,
                    Comparison = Comparison.Equal
                },
                BackFace = new DepthStencilOperationDescription {
                    FailOperation = StencilOperation.Keep,
                    DepthFailOperation = StencilOperation.Keep,
                    PassOperation = StencilOperation.Keep,
                    Comparison = Comparison.Equal
                }
            };
            DrawReflectionDSS = DepthStencilState.FromDescription(device, drawReflectionDesc);

            var noDoubleBlendDesc = new DepthStencilStateDescription {
                IsDepthEnabled = true,
                DepthWriteMask = DepthWriteMask.All,
                DepthComparison = Comparison.Less,
                IsStencilEnabled = true,
                StencilReadMask = 0xff,
                StencilWriteMask = 0xff,
                FrontFace = new DepthStencilOperationDescription {
                    FailOperation = StencilOperation.Keep,
                    DepthFailOperation = StencilOperation.Keep,
                    PassOperation = StencilOperation.Increment,
                    Comparison = Comparison.Equal
                },
                BackFace = new DepthStencilOperationDescription {
                    FailOperation = StencilOperation.Keep,
                    DepthFailOperation = StencilOperation.Keep,
                    PassOperation = StencilOperation.Increment,
                    Comparison = Comparison.Equal
                }
            };
            NoDoubleBlendDSS = DepthStencilState.FromDescription(device, noDoubleBlendDesc);


        }
        public static void DestroyAll() {
            Util.ReleaseCom(ref WireframeRS);
            Util.ReleaseCom(ref NoCullRS);
            Util.ReleaseCom(ref CullClockwiseRS);
            Util.ReleaseCom(ref AlphaToCoverageBS);
            Util.ReleaseCom(ref TransparentBS);
            Util.ReleaseCom(ref NoRenderTargetWritesBS);
            Util.ReleaseCom(ref MarkMirrorDSS);
            Util.ReleaseCom(ref DrawReflectionDSS);
            Util.ReleaseCom(ref NoDoubleBlendDSS);
        }

        public static RasterizerState WireframeRS;
        public static RasterizerState NoCullRS;
        public static RasterizerState CullClockwiseRS;

        public static BlendState AlphaToCoverageBS;
        public static BlendState TransparentBS;
        public static BlendState NoRenderTargetWritesBS;

        public static DepthStencilState MarkMirrorDSS;
        public static DepthStencilState DrawReflectionDSS;
        public static DepthStencilState NoDoubleBlendDSS;

    }
}
