using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Core;
using SlimDX;
using SlimDX.D3DCompiler;
using SlimDX.DXGI;
using SlimDX.Direct3D11;
using Buffer = SlimDX.Direct3D11.Buffer;
using Debug = System.Diagnostics.Debug;

namespace BoxDemo {
    public class BoxApp : D3DApp {
    private Buffer _boxVB;
    private Buffer _boxIB;

    private Effect _fx;
    private EffectTechnique _tech;
    private EffectMatrixVariable _fxWVP;

    private InputLayout _inputLayout;

    // Matrices
    private Matrix _world;
    private Matrix _view;
    private Matrix _proj;

    // Camera variables
    private float _theta;
    private float _phi;
    private float _radius;

    private Point _lastMousePos;

    private bool _disposed;

    public BoxApp(IntPtr hInstance) : base(hInstance) {
        _boxIB = null;
        _boxVB = null;
        _fx = null;
        _tech = null;
        _fxWVP = null;
        _inputLayout = null;
        _theta = 1.5f * MathF.PI;
        _phi = 0.25f * MathF.PI;
        _radius = 5.0f;

        MainWindowCaption = "Box Demo";
        _lastMousePos = new Point(0, 0);
        _world = Matrix.Identity;
        _view = Matrix.Identity;
        _proj = Matrix.Identity;
    }
    protected override void Dispose(bool disposing) {
        if (!_disposed) {
            if (disposing) {
                Util.ReleaseCom(_boxVB);
                Util.ReleaseCom(_boxIB);
                Util.ReleaseCom(_fx);
                Util.ReleaseCom(_inputLayout);
            }
            _disposed = true;
        }
        base.Dispose(disposing);
    }

    public override bool Init() {
        if (!base.Init()) {
            return false;
        }
        BuildGeometryBuffers();
        BuildFX();
        BuildVertexLayout();

        return true;
    }
    public override void OnResize() {
        base.OnResize();
        // Recalculate perspective matrix
        _proj = Matrix.PerspectiveFovLH(0.25f * MathF.PI, AspectRatio, 1.0f, 1000.0f);
    }
    public override void UpdateScene(float dt) {
        base.UpdateScene(dt);

        // Get camera position from polar coords
        var x = _radius * MathF.Sin(_phi) * MathF.Cos(_theta);
        var z = _radius * MathF.Sin(_phi) * MathF.Sin(_theta);
        var y = _radius * MathF.Cos(_phi);

        // Build the view matrix
        var pos = new Vector3(x, y, z);
        var target = new Vector3(0);
        var up = new Vector3(0, 1, 0);
        _view = Matrix.LookAtLH(pos, target, up);

    }
    public override void DrawScene() {
        base.DrawScene();
        ImmediateContext.ClearRenderTargetView(RenderTargetView, Color.LightSteelBlue);
        ImmediateContext.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);

        ImmediateContext.InputAssembler.InputLayout = _inputLayout;
        ImmediateContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

        ImmediateContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_boxVB, Vertex.Stride, 0));
        ImmediateContext.InputAssembler.SetIndexBuffer(_boxIB, Format.R32_UInt, 0);

        var wvp = _world * _view * _proj;
        _fxWVP.SetMatrix(wvp);

        for (int p = 0; p < _tech.Description.PassCount; p++) {
            _tech.GetPassByIndex(p).Apply(ImmediateContext);
            ImmediateContext.DrawIndexed(36, 0, 0);
        }
        SwapChain.Present(0, PresentFlags.None);

    }

    protected override void OnMouseDown(object sender, MouseEventArgs mouseEventArgs) {
        _lastMousePos = mouseEventArgs.Location;
        Window.Capture = true;
    }
    protected override void OnMouseUp(object sender, MouseEventArgs e) {
        Window.Capture = false;
    }
    protected override void OnMouseMove(object sender, MouseEventArgs e) {
        if (e.Button == MouseButtons.Left) {
            var dx = MathF.ToRadians(0.25f * (e.X - _lastMousePos.X));
            var dy = MathF.ToRadians(0.25f * (e.Y - _lastMousePos.Y));

            _theta += dx;
            _phi += dy;

            _phi = MathF.Clamp(_phi, 0.1f, MathF.PI - 0.1f);
        } else if (e.Button == MouseButtons.Right) {
            var dx = 0.005f * (e.X - _lastMousePos.X);
            var dy = 0.005f * (e.Y - _lastMousePos.Y);
            _radius += dx - dy;

            _radius = MathF.Clamp(_radius, 3.0f, 15.0f);
        }
        _lastMousePos = e.Location;
    }

    private void BuildGeometryBuffers() {
        var vertices = new[] {
            new Vertex(new Vector3(-1.0f, -1.0f, -1.0f), Color.White),
            new Vertex(new Vector3(-1, 1, -1), Color.Black),
            new Vertex(new Vector3(1,1,-1), Color.Red ),
            new Vertex( new Vector3(1,-1,-1), Color.Green ),
            new Vertex(new Vector3(-1,-1,1),Color.Blue ),
            new Vertex(new Vector3(-1,1,1), Color.Yellow ),
            new Vertex(new Vector3(1,1,1), Color.Cyan ),
            new Vertex(new Vector3(1,-1,1),Color.Magenta )
        };
        var vbd = new BufferDescription(
            Vertex.Stride*vertices.Length, 
            ResourceUsage.Immutable, 
            BindFlags.VertexBuffer, 
            CpuAccessFlags.None, 
            ResourceOptionFlags.None, 
            0);
        _boxVB = new Buffer(Device, new DataStream(vertices, true, false), vbd);

        var indices = new uint[] {
            // front
            0,1,2,
            0,2,3,
            // back
            4,6,5,
            4,7,6,
            // left
            4,5,1,
            4,1,0,
            // right
            3,2,6,
            3,6,7,
            //top
            1,5,6,
            1,6,2,
            // bottom
            4,0,3,
            4,3,7
        };
        var ibd = new BufferDescription(
            sizeof (uint)*indices.Length, 
            ResourceUsage.Immutable, 
            BindFlags.IndexBuffer, 
            CpuAccessFlags.None, 
            ResourceOptionFlags.None, 
            0);
        _boxIB = new Buffer(Device, new DataStream(indices, false, false), ibd);
    }
    private void BuildFX() {
        var shaderFlags = ShaderFlags.None;
        #if DEBUG
            shaderFlags |= ShaderFlags.Debug;
            shaderFlags |= ShaderFlags.SkipOptimization;
        #endif
        string errors = null;
        ShaderBytecode compiledShader = null;
        try {
            compiledShader = ShaderBytecode.CompileFromFile(
                "FX/color.fx", 
                null, 
                "fx_5_0", 
                shaderFlags, 
                EffectFlags.None, 
                null, 
                null, 
                out errors);
            _fx = new Effect(Device, compiledShader);
        } catch (Exception ex) {
            if (!string.IsNullOrEmpty(errors)) {
                MessageBox.Show(errors);
            }
            MessageBox.Show(ex.Message);
            return;
        } finally {
            Util.ReleaseCom(compiledShader);
        }

        _tech = _fx.GetTechniqueByName("ColorTech");
        _fxWVP = _fx.GetVariableByName("gWorldViewProj").AsMatrix();
    }
    private void BuildVertexLayout() {
        var vertexDesc = new[] {
            new InputElement("POSITION", 0, Format.R32G32B32_Float, 
                0, 0, InputClassification.PerVertexData, 0),
            new InputElement("COLOR", 0, Format.R32G32B32A32_Float, 
                12, 0, InputClassification.PerVertexData, 0)
        };
        Debug.Assert(_tech != null);
        var passDesc = _tech.GetPassByIndex(0).Description;
        _inputLayout = new InputLayout(Device, passDesc.Signature, vertexDesc);
    }
}


    static class Program {
        static void Main(string[] args) {
            Configuration.EnableObjectTracking = true;
            var app = new BoxApp(Process.GetCurrentProcess().Handle);
            if (!app.Init()) {
                return;
            }
            app.Run();
        }
    }
}
