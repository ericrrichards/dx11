using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InitDirect3D {
    using System.Diagnostics;
    using System.Drawing;

    using Core;

    using SlimDX.DXGI;
    using SlimDX.Direct3D11;

    using Debug = System.Diagnostics.Debug;

class InitDirect3D : D3DApp {
    private bool _disposed;
    public InitDirect3D(IntPtr hInstance) : base(hInstance) {
    }
    protected override void Dispose(bool disposing) {
        if (!_disposed) {
            if (disposing) {
                    
            }
            _disposed = true;
        }
        base.Dispose(disposing);
    }
        
    public override void DrawScene() {
        Debug.Assert(ImmediateContext!= null);
        Debug.Assert(SwapChain != null);
        ImmediateContext.ClearRenderTargetView(RenderTargetView, Color.Blue);
        ImmediateContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth|DepthStencilClearFlags.Stencil, 1.0f, 0);

        SwapChain.Present(0, PresentFlags.None);
    }
}

class Program {
    static void Main(string[] args) {
        SlimDX.Configuration.EnableObjectTracking = true;
        var app = new InitDirect3D(Process.GetCurrentProcess().Handle);
        if (!app.Init()) {
            return;
        }
        app.Run();
    }
}
}
