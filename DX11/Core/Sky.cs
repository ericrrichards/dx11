using System.Linq;

namespace Core {
    using System.Runtime.InteropServices;

    using Core.Camera;
    using Core.FX;

    using SlimDX;
    using SlimDX.DXGI;
    using SlimDX.Direct3D11;

    using Device = SlimDX.Direct3D11.Device;

    public class Sky : DisposableClass {
        private Buffer _vb;
        private Buffer _ib;
        private ShaderResourceView _cubeMapSRV;
        public ShaderResourceView CubeMapSRV { get { return _cubeMapSRV; } private set { _cubeMapSRV = value; } }
        private readonly int _indexCount;
        private bool _disposed;

        public Sky(Device device, string filename, float skySphereRadius) {
            CubeMapSRV = ShaderResourceView.FromFile(device, filename);
            using (var r = CubeMapSRV.Resource) {
                r.DebugName = "sky cubemap";
            }
            
            var sphere = GeometryGenerator.CreateSphere(skySphereRadius, 30, 30);
            var vertices = sphere.Vertices.Select(v => v.Position).ToArray();
            var vbd = new BufferDescription(
                Marshal.SizeOf(typeof(Vector3)) * vertices.Length, 
                ResourceUsage.Immutable, 
                BindFlags.VertexBuffer, 
                CpuAccessFlags.None, 
                ResourceOptionFlags.None, 
                0
            );
            _vb = new Buffer(device, new DataStream(vertices, false, false), vbd);

            _indexCount = sphere.Indices.Count;
            var ibd = new BufferDescription(
                _indexCount * sizeof(int), 
                ResourceUsage.Immutable, 
                BindFlags.IndexBuffer, 
                CpuAccessFlags.None, 
                ResourceOptionFlags.None, 
                0
            );
            _ib = new Buffer(device, new DataStream(sphere.Indices.ToArray(), false, false), ibd);

        }
        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Util.ReleaseCom(ref _vb);
                    Util.ReleaseCom(ref _ib);
                    Util.ReleaseCom(ref _cubeMapSRV);
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }
        public void Draw(DeviceContext dc, CameraBase camera) {
            var eyePos = camera.Position;
            var t = Matrix.Translation(eyePos);
            var wvp = t * camera.ViewProj;

            Effects.SkyFX.SetWorldViewProj(wvp);
            Effects.SkyFX.SetCubeMap(_cubeMapSRV);

            var stride = Marshal.SizeOf(typeof(Vector3));
            const int Offset = 0;
            dc.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vb, stride, Offset));
            dc.InputAssembler.SetIndexBuffer(_ib, Format.R32_UInt, 0);
            dc.InputAssembler.InputLayout = InputLayouts.Pos;
            dc.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            var tech = Effects.SkyFX.SkyTech;
            for (var p = 0; p < tech.Description.PassCount; p++) {
                var pass = tech.GetPassByIndex(p);
                pass.Apply(dc);
                dc.DrawIndexed(_indexCount, 0, 0);
            }
        }
    }
}
