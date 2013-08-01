using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX.Direct3D11;

using Debug = System.Diagnostics.Debug;

namespace Core {
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

        }
        public static void DestroyAll() {
            Util.ReleaseCom(ref WireframeRS);
            Util.ReleaseCom(ref NoCullRS);
            Util.ReleaseCom(ref AlphaToCoverageBS);
            Util.ReleaseCom(ref TransparentBS);
        }

        public static RasterizerState WireframeRS;
        public static RasterizerState NoCullRS;

        public static BlendState AlphaToCoverageBS;
        public static BlendState TransparentBS;

    }
}
