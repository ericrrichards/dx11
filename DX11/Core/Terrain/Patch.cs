using System.Collections.Generic;
using System.Drawing;
using Core.Camera;
using SlimDX;
using SlimDX.DXGI;
using SlimDX.Direct3D11;
using Device = SlimDX.Direct3D11.Device;

namespace Core.Terrain {
    class Patch : DisposableClass {
        private Buffer _vb;
        private Buffer _ib;
        private int _numIndices;
        public BoundingBox Bounds { get; private set; }

        public void CreateMesh(Terrain terrain, Rectangle r, Device device) {
            if (_vb != null) {
                Util.ReleaseCom(ref _vb);
                _vb = null;
            }
            if (_ib != null) {
                Util.ReleaseCom(ref _ib);
                _ib = null;
            }
            var width = r.Width;
            var height = r.Height;
            var nrVert = (width + 1)*(height + 1);
            var nrTri = width*height*2;
            var halfWidth = 0.5f*terrain.Width;
            var halfDepth = 0.5f*terrain.Depth;

            var patchWidth = terrain.Width/(terrain._numPatchVertCols - 1);
            var patchDepth = terrain.Depth/(terrain._numPatchVertRows - 1);
            var vertWidth =  terrain._info.CellSpacing;
            var vertDepth =  terrain._info.CellSpacing;
            var du = 1.0f/(terrain._numPatchVertCols - 1) / Terrain.CellsPerPatch;
            var dv = 1.0f/(terrain._numPatchVertRows - 1) / Terrain.CellsPerPatch;

            var verts = new List<Vertex.TerrainCP>();
            var min = new Vector3(float.MaxValue);
            var max = new Vector3(float.MinValue);
            for (int z = r.Top, z0 = 0; z <= r.Bottom; z++, z0++) {
                var zp = halfDepth - r.Top / Terrain.CellsPerPatch * patchDepth - z0 * vertDepth;

                for (int x = r.Left, x0 = 0; x <= r.Right; x++, x0++ ) {
                    var xp = -halfWidth + r.Left/Terrain.CellsPerPatch*patchWidth + x0*vertWidth;
                    var pos = new Vector3(xp, terrain.Height(xp, zp), zp);

                    min = Vector3.Minimize(min, pos);
                    max = Vector3.Maximize(max, pos);

                    var uv = new Vector2(r.Left*du + x0*du, r.Top * dv + z0*dv);
                    var v = new Vertex.TerrainCP(pos, uv, new Vector2());
                    verts.Add(v);

                }
            }

            Bounds = new BoundingBox(min, max);

            var vbd = new BufferDescription(
                Vertex.TerrainCP.Stride*verts.Count, 
                ResourceUsage.Immutable, 
                BindFlags.VertexBuffer, 
                CpuAccessFlags.None, 
                ResourceOptionFlags.None, 
                0
            );
            _vb = new Buffer(device, new DataStream(verts.ToArray(), false, false), vbd);
            var indices = new List<int>();
            for (int z = r.Top, z0 = 0; z < r.Bottom; z++, z0++) {
                for (int x = r.Left, x0 = 0; x < r.Right; x++, x0++) {
                    indices.Add(z0 * (width + 1) + x0);
                    indices.Add(z0 * (width + 1) + x0+1);
                    indices.Add((z0 + 1) * (width + 1) + x0);

                    indices.Add((z0+1) * (width + 1) + x0);
                    indices.Add(z0 * (width + 1) + x0+1);
                    indices.Add((z0+1) * (width + 1) + x0+1);
                }
            }
            _numIndices = indices.Count;
            var ibd = new BufferDescription(
                sizeof (int)*indices.Count, 
                ResourceUsage.Dynamic, 
                BindFlags.IndexBuffer, 
                CpuAccessFlags.Write, 
                ResourceOptionFlags.None, 
                0
            );

            _ib = new Buffer(device, new DataStream(indices.ToArray(), false, true), ibd);

        }

        public void Draw(DeviceContext dc) {
            dc.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vb, Vertex.TerrainCP.Stride, 0));
            dc.InputAssembler.SetIndexBuffer(_ib, Format.R32_UInt, 0);


            dc.DrawIndexed(_numIndices, 0, 0);
        }
    }
}