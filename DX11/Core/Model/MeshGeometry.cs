

using System.Collections.Generic;
using System.Runtime.InteropServices;
using SlimDX;
using SlimDX.DXGI;
using SlimDX.Direct3D11;
using Device = SlimDX.Direct3D11.Device;

namespace Core.Model {
    public class MeshGeometry : DisposableClass {
        public class Subset {
            public int VertexStart;
            public int VertexCount;
            public int FaceStart;
            public int FaceCount;
        }

        private Buffer _vb;
        private Buffer _ib;
        private int _vertexStride;
        private List<Subset> _subsetTable;
        private bool _disposed;

        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    Util.ReleaseCom(ref _vb);
                    Util.ReleaseCom(ref _ib);
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }
        public void SetVertices<TVertexType>(Device device, List<TVertexType> vertices) where TVertexType : struct {
            Util.ReleaseCom(ref _vb);
            _vertexStride = Marshal.SizeOf(typeof (TVertexType));

            var vbd = new BufferDescription(
                _vertexStride*vertices.Count, 
                ResourceUsage.Immutable, 
                BindFlags.VertexBuffer, 
                CpuAccessFlags.None, 
                ResourceOptionFlags.None, 
                0
            );
            _vb = new Buffer(device, new DataStream(vertices.ToArray(), false, false), vbd);
        }
        public void SetIndices(Device device, List<int> indices) {
            var ibd = new BufferDescription(
                sizeof (int)*indices.Count, 
                ResourceUsage.Immutable, 
                BindFlags.IndexBuffer, 
                CpuAccessFlags.None, 
                ResourceOptionFlags.None, 
                0
            );
            _ib = new Buffer(device, new DataStream(indices.ToArray(), false, false), ibd);
        }
        public void SetSubsetTable(List<Subset> subsets) {
            _subsetTable = subsets;
        }
        public void Draw(DeviceContext dc, int subsetId) {
            const int offset = 0;
            dc.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vb, _vertexStride, offset));
            dc.InputAssembler.SetIndexBuffer(_ib, Format.R32_UInt, 0);
            dc.DrawIndexed(_subsetTable[subsetId].FaceCount*3, _subsetTable[subsetId].FaceStart*3, 0);
        }
        public void DrawInstanced(DeviceContext dc, int subsetId, Buffer instanceBuffer, int numInstances, int instanceStride) {
            const int offset = 0;
            dc.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vb, _vertexStride, offset), new VertexBufferBinding(instanceBuffer, instanceStride, 0));
            dc.InputAssembler.SetIndexBuffer(_ib, Format.R32_UInt, 0);
            dc.DrawIndexedInstanced(_subsetTable[subsetId].FaceCount * 3, numInstances, _subsetTable[subsetId].FaceStart * 3,  0, 0);
        }
    }
}
