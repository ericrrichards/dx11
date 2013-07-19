using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX.Direct3D11;

namespace ShapesDemo {
    using System.Windows.Forms;

    using Core;

    using SlimDX;

    using Buffer = SlimDX.Direct3D11.Buffer;

    class ShapesDemo : D3DApp {
        private Buffer _vb;
        private Buffer _ib;

        private Effect _fx;
        private EffectTechnique _tech;
        private EffectMatrixVariable _fxWVP;

        private InputLayout _inputLayout;

        private RasterizerState _wireframeRS;
        private Matrix[] _sphereWorld = new Matrix[10];
        private Matrix[] _cylWorld = new Matrix[10];
        private Matrix _boxWorld;
        private Matrix _gridWorld;
        private Matrix _centerSphere;

        private bool _disposed;
        public ShapesDemo(IntPtr hInstance) : base(hInstance) {

        }
        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }
        public override bool Init() {
            return base.Init();
        }
        public override void OnResize() {
            base.OnResize();
        }
        public override void UpdateScene(float dt) {
            base.UpdateScene(dt);
        }
        public override void DrawScene() {
            base.DrawScene();
        }
        protected override void OnMouseDown(object sender, MouseEventArgs mouseEventArgs) {
            base.OnMouseDown(sender, mouseEventArgs);
        }
        protected override void OnMouseUp(object sender, MouseEventArgs e) {
            base.OnMouseUp(sender, e);
        }
        protected override void OnMouseMove(object sender, MouseEventArgs e) {
            base.OnMouseMove(sender, e);
        }

        private void BuildGeometryBuffers() {
            
        }
        private void BuildFX() {
            
        }
        private void BuildVertexLayout() {
            
        }
    }

    class Program {
        static void Main(string[] args) {
        }
    }
}
