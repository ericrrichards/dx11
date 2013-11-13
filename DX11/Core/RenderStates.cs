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
                StencilWriteMask = 0xff, 
                FrontFace = new DepthStencilOperationDescription {
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

            var lessEqualDesc = new DepthStencilStateDescription {
                IsDepthEnabled = true,
                DepthWriteMask = DepthWriteMask.All,
                DepthComparison = Comparison.LessEqual,
                IsStencilEnabled = false
            };
            LessEqualDSS = DepthStencilState.FromDescription(device, lessEqualDesc);

            var equalsDesc = new DepthStencilStateDescription() {
                IsDepthEnabled = true,
                DepthWriteMask = DepthWriteMask.Zero,
                DepthComparison = Comparison.LessEqual,
                
            };
            EqualsDSS = DepthStencilState.FromDescription(device, equalsDesc);

            var noDepthDesc = new DepthStencilStateDescription() {
                IsDepthEnabled = false,
                DepthComparison = Comparison.Always,
                DepthWriteMask = DepthWriteMask.Zero
            };
            NoDepthDSS = DepthStencilState.FromDescription(device, noDepthDesc);
        }
        public static void DestroyAll() {
            var rasterizerState = WireframeRS;
            Util.ReleaseCom(ref rasterizerState);
            var noCullRS = NoCullRS;
            Util.ReleaseCom(ref noCullRS);
            var cullClockwiseRS = CullClockwiseRS;
            Util.ReleaseCom(ref cullClockwiseRS);
            var alphaToCoverageBS = AlphaToCoverageBS;
            Util.ReleaseCom(ref alphaToCoverageBS);
            var transparentBS = TransparentBS;
            Util.ReleaseCom(ref transparentBS);
            var noRenderTargetWritesBS = NoRenderTargetWritesBS;
            Util.ReleaseCom(ref noRenderTargetWritesBS);
            var depthStencilState = MarkMirrorDSS;
            Util.ReleaseCom(ref depthStencilState);
            var drawReflectionDSS = DrawReflectionDSS;
            Util.ReleaseCom(ref drawReflectionDSS);
            var noDoubleBlendDSS = NoDoubleBlendDSS;
            Util.ReleaseCom(ref noDoubleBlendDSS);
            var lessEqualDSS = LessEqualDSS;
            Util.ReleaseCom(ref lessEqualDSS);
            var stencilState = EqualsDSS;
            Util.ReleaseCom(ref stencilState);
            var noDepthDSS = NoDepthDSS;
            Util.ReleaseCom(ref noDepthDSS);
        }

        public static RasterizerState WireframeRS { get; private set; }
        public static RasterizerState NoCullRS { get; private set; }
        public static RasterizerState CullClockwiseRS { get; private set; }

        public static BlendState AlphaToCoverageBS { get; private set; }
        public static BlendState TransparentBS { get; private set; }
        public static BlendState NoRenderTargetWritesBS { get; private set; }

        public static DepthStencilState MarkMirrorDSS { get; private set; }
        public static DepthStencilState DrawReflectionDSS { get; private set; }
        public static DepthStencilState NoDoubleBlendDSS { get; private set; }
        public static DepthStencilState LessEqualDSS { get; private set; }
        public static DepthStencilState EqualsDSS { get; private set; }
        public static DepthStencilState NoDepthDSS { get; private set; }
    }
}
