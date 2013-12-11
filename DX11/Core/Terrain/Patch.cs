using System.Collections.Generic;
using System.Drawing;

using SlimDX;
using SlimDX.DXGI;
using SlimDX.Direct3D11;
using Device = SlimDX.Direct3D11.Device;

namespace Core.Terrain {
    using System;

    using Vertex;

    using Buffer = Buffer;

    public enum NeighborDir {
        Top, Left
    }

    public class Patch : DisposableClass {

        private static readonly Dictionary<int, Buffer> IB = new Dictionary<int, Buffer>();
        private static readonly Dictionary<int, int> IndexCount = new Dictionary<int, int>();
        private static readonly Dictionary<int, Buffer> CenterIB = new Dictionary<int, Buffer>();
        private static readonly Dictionary<int, int> CenterIndexCount = new Dictionary<int, int>();
        private static readonly Dictionary<NeighborDir, Dictionary<Tuple<int, int>, Buffer>> EdgeIbs = 
            new Dictionary<NeighborDir, Dictionary<Tuple<int, int>, Buffer>>();
        private static readonly Dictionary<NeighborDir, Dictionary<Tuple<int, int>, int>> EdgeIndiceCount = 
            new Dictionary<NeighborDir, Dictionary<Tuple<int, int>, int>>();
        private static int width;

        public BoundingBox Bounds { get; private set; }
        private bool _disposed;
        private List<TerrainCP> _verts;
        private Buffer _vb;

        protected override void Dispose(bool disposing) {
            if (_disposed) return;
            if (disposing) {
                // free IDisposable objects
                Util.ReleaseCom(ref _vb);
                
            }
            // release unmanaged objects
            _disposed = true;

        }

        private int TessFactor(Vector3 eye) {
            var c = (Bounds.Maximum - Bounds.Minimum) / 2 + Bounds.Minimum;
            var d = Vector3.Distance(eye, c);
            var s = MathF.Clamp(-(d - TerrainRenderer.MaxDist) / (TerrainRenderer.MaxDist - TerrainRenderer.MinDist), 0, 1);
            s = 1.0f - s;
            
            return (int)Math.Pow(2, (int)(TerrainRenderer.MinTess + (TerrainRenderer.MaxTess-1 - TerrainRenderer.MinTess) * s));
        }
        public static void InitPatchData(int patchWidth, Device device) {
            width = patchWidth;
            BuildCenterIndices(device);
            BuildTopEdges(device);
            BuildLeftEdges(device);
        }
        public static void DestroyPatchIndexBuffers() {
            foreach (var buffer in CenterIB) {
                var buf = buffer.Value;
                Util.ReleaseCom(ref buf);
            }
            foreach (var edgeIb in EdgeIbs) {
                foreach (var buffer in edgeIb.Value) {
                    var buf = buffer.Value;
                    Util.ReleaseCom(ref buf);
                }
            }
            foreach (var buffer in IB) {
                var buf = buffer.Value;
                Util.ReleaseCom(ref buf);
            }
        }

        public void CreateMesh(Terrain terrain, Rectangle r, Device device) {
            if (_vb != null) {
                Util.ReleaseCom(ref _vb);
                _vb = null;
            }
            
            var halfWidth = 0.5f * terrain.Width;
            var halfDepth = 0.5f * terrain.Depth;

            var patchWidth = terrain.Width / (terrain.Renderer.NumPatchVertCols - 1);
            var patchDepth = terrain.Depth / (terrain.Renderer.NumPatchVertRows - 1);
            var vertWidth = terrain.Info.CellSpacing;
            var vertDepth = terrain.Info.CellSpacing;
            var du = 1.0f / (terrain.Renderer.NumPatchVertCols - 1) / Terrain.CellsPerPatch;
            var dv = 1.0f / (terrain.Renderer.NumPatchVertRows - 1) / Terrain.CellsPerPatch;

            _verts = new List<TerrainCP>();
            var min = new Vector3(float.MaxValue);
            var max = new Vector3(float.MinValue);
            for (int z = r.Top, z0 = 0; z <= r.Bottom; z++, z0++) {
                var zp = halfDepth - r.Top / Terrain.CellsPerPatch * patchDepth - z0 * vertDepth;

                for (int x = r.Left, x0 = 0; x <= r.Right; x++, x0++) {
                    var xp = -halfWidth + r.Left / Terrain.CellsPerPatch * patchWidth + x0 * vertWidth;
                    var pos = new Vector3(xp, terrain.Height(xp, zp), zp);

                    min = Vector3.Minimize(min, pos);
                    max = Vector3.Maximize(max, pos);

                    var uv = new Vector2(r.Left * du + x0 * du, r.Top * dv + z0 * dv);
                    var v = new TerrainCP(pos, uv, new Vector2());
                    _verts.Add(v);
                }
            }

            Bounds = new BoundingBox(min, max);

            var vbd = new BufferDescription(
                TerrainCP.Stride * _verts.Count,
                ResourceUsage.Immutable,
                BindFlags.VertexBuffer,
                CpuAccessFlags.None,
                ResourceOptionFlags.None,
                0
            );
            _vb = new Buffer(device, new DataStream(_verts.ToArray(), false, false), vbd);
        }

        private static void BuildCenterIndices(Device device) {
            for (var tessLevel = 0; tessLevel <= 6; tessLevel++) {
                var t = (int)Math.Pow(2, tessLevel);
                var indices = new List<short>();
                for (int z = 0 + t, z0 = t; z < width; z += t, z0 += t) {
                    for (int x = 0 + t, x0 = t; x < width; x += t, x0 += t) {
                        indices.Add((short)(z0 * (width + 1) + x0));
                        indices.Add((short)(z0 * (width + 1) + x0 + t));
                        indices.Add((short)((z0 + t) * (width + 1) + x0));

                        indices.Add((short)((z0 + t) * (width + 1) + x0));
                        indices.Add((short)(z0 * (width + 1) + x0 + t));
                        indices.Add((short)((z0 + t) * (width + 1) + x0 + t));
                    }
                }

                var ibd = new BufferDescription(
                    sizeof(short) * indices.Count,
                    ResourceUsage.Dynamic,
                    BindFlags.IndexBuffer,
                    CpuAccessFlags.Write,
                    ResourceOptionFlags.None,
                    0
                    );

                if (CenterIB != null) {
                    if (indices.Count > 0) {
                        CenterIB[t] = new Buffer(
                            device, 
                            new DataStream(indices.ToArray(), false, true), 
                            ibd
                        );
                    } else {
                        CenterIB[t] = null;
                    }
                }
                CenterIndexCount[t] = indices.Count;
            }
        }
        private static void BuildIndices(Device device) {
            for (var tessLevel = 0; tessLevel <= 6; tessLevel++) {
                var t = (int)Math.Pow(2, tessLevel);
                var indices = new List<short>();
                for (int z = 0, z0 = 0; z < width; z += t, z0 += t) {
                    for (int x = 0, x0 = 0; x < width; x += t, x0 += t) {
                        indices.Add((short)(z0 * (width + 1) + x0));
                        indices.Add((short)(z0 * (width + 1) + x0 + t));
                        indices.Add((short)((z0 + t) * (width + 1) + x0));

                        indices.Add((short)((z0 + t) * (width + 1) + x0));
                        indices.Add((short)(z0 * (width + 1) + x0 + t));
                        indices.Add((short)((z0 + t) * (width + 1) + x0 + t));
                    }
                }

                var ibd = new BufferDescription(
                    sizeof(short) * indices.Count,
                    ResourceUsage.Dynamic,
                    BindFlags.IndexBuffer,
                    CpuAccessFlags.Write,
                    ResourceOptionFlags.None,
                    0
                    );

                if (IB != null) {
                    if (indices.Count > 0) {
                        IB[t] = new Buffer(
                            device,
                            new DataStream(indices.ToArray(), false, true),
                            ibd
                        );
                    } else {
                        IB[t] = null;
                    }
                }
                IndexCount[t] = indices.Count;
            }
        }

        private static void BuildLeftEdges(Device device) {
            BufferDescription ibd;
            EdgeIbs[NeighborDir.Left] = new Dictionary<Tuple<int, int>, Buffer>();
            EdgeIndiceCount[NeighborDir.Left] = new Dictionary<Tuple<int, int>, int>();

            Tuple<int, int> key;
            List<short> indices;
            int x0;
            int t;
            for (var i = 0; i < 6; i++) {
                t = (int)Math.Pow(2, i);
                key = new Tuple<int, int>(t, t);
                indices = new List<short>();
                x0 = 0;
                for (int z = 0, z0 = 0; z < width; z += t, z0 += t) {
                    indices.Add((short)(z0 * (width + 1) + x0));
                    indices.Add((short)(z0 * (width + 1) + x0 + t));
                    indices.Add((short)((z0 + t) * (width + 1) + x0));

                    indices.Add((short)((z0 + t) * (width + 1) + x0));
                    indices.Add((short)(z0 * (width + 1) + x0 + t));
                    indices.Add((short)((z0 + t) * (width + 1) + x0 + t));
                }

                ibd = new BufferDescription(
                    sizeof(short) * indices.Count,
                    ResourceUsage.Dynamic,
                    BindFlags.IndexBuffer,
                    CpuAccessFlags.Write,
                    ResourceOptionFlags.None,
                    0
                    );
                EdgeIbs[NeighborDir.Left][key] = new Buffer(device, new DataStream(indices.ToArray(), false, false), ibd);
                EdgeIndiceCount[NeighborDir.Left][key] = indices.Count;
            }
            for (var i = 0; i < 6; i++) {
                t = (int)Math.Pow(2, i);
                var t1 = (int)Math.Pow(2, i + 1);
                key = new Tuple<int, int>(t, t1);
                indices = new List<short>();
                x0 = 0;

                indices.Add(0);
                indices.Add((short)((width+1) + t));
                indices.Add((short)(t1*(width+1)));

                indices.Add((short)((width + 1) + t));
                indices.Add((short)(t1 * (width + 1) + t));
                indices.Add((short)(t1 * (width + 1)));

                
                for (int z = 0+t1, z0 = t1; z < width; z += t1, z0 += t1) {
                    indices.Add((short)(z0 * (width + 1) + x0));
                    indices.Add((short)(z0 * (width + 1) + x0 + t));
                    indices.Add((short)((z0 + t) * (width + 1) + x0 + t));

                    indices.Add((short)((z0 + t) * (width + 1) + x0 + t));
                    indices.Add((short)((z0 + t1) * (width + 1) + x0 + t));
                    indices.Add((short)((z0 + t1) * (width + 1) + x0));

                    indices.Add((short)(z0 * (width + 1) + x0));
                    indices.Add((short)((z0 + t) * (width + 1) + x0 + t));
                    indices.Add((short)((z0 + t1) * (width + 1) + x0));
                }
                
                ibd = new BufferDescription(
                    sizeof(short) * indices.Count,
                    ResourceUsage.Dynamic,
                    BindFlags.IndexBuffer,
                    CpuAccessFlags.Write,
                    ResourceOptionFlags.None,
                    0
                    );
                EdgeIbs[NeighborDir.Left][key] = new Buffer(
                    device, new DataStream(indices.ToArray(), false, false), ibd);
                EdgeIndiceCount[NeighborDir.Left][key] = indices.Count;
            }
            
            for (var i = 1; i <= 6; i++) {
                t = (int)Math.Pow(2, i);
                var t1 = (int)Math.Pow(2, i - 1);
                key = new Tuple<int, int>(t, t1);
                indices = new List<short>();
                x0 = 0;

                for (int z = 0 , z0 = 0; z < width - t1; z += t, z0 += t) {
                    indices.Add((short)(z0 * (width + 1) + x0));
                    indices.Add((short)((z0) * (width + 1) + x0 + t));
                    indices.Add((short)((z0 + t1) * (width + 1) + x0));

                    indices.Add((short)((z0 + t1) * (width + 1) + x0));
                    indices.Add((short)((z0 + t) * (width + 1) + x0 + t));
                    indices.Add((short)((z0 + t) * (width + 1) + x0));

                    indices.Add((short)((z0) * (width + 1) + x0 + t));
                    indices.Add((short)((z0 + t) * (width + 1) + x0 + t));
                    indices.Add((short)((z0 + t1) * (width + 1) + x0));
                }

                ibd = new BufferDescription(
                    sizeof(short) * indices.Count,
                    ResourceUsage.Dynamic,
                    BindFlags.IndexBuffer,
                    CpuAccessFlags.Write,
                    ResourceOptionFlags.None,
                    0
                    );
                EdgeIbs[NeighborDir.Left][key] = new Buffer(device, new DataStream(indices.ToArray(), false, false), ibd);
                EdgeIndiceCount[NeighborDir.Left][key] = indices.Count;
            }
        }

        private static void BuildTopEdges(Device device) {
            BufferDescription ibd;
            EdgeIbs[NeighborDir.Top] = new Dictionary<Tuple<int, int>, Buffer>();
            EdgeIndiceCount[NeighborDir.Top] = new Dictionary<Tuple<int, int>, int>();

            Tuple<int, int> key;
            List<short> indices;
            int z0;
            int t;
            for (var i = 0; i < 6; i++) {
                t = (int)Math.Pow(2, i);
                key = new Tuple<int, int>(t, t);
                indices = new List<short>();
                z0 = 0;
                for (int x = 0, x0 = 0; x < width; x += t, x0 += t) {
                    indices.Add((short)(z0 * (width + 1) + x0));
                    indices.Add((short)(z0 * (width + 1) + x0 + t));
                    indices.Add((short)((z0 + t) * (width + 1) + x0));

                    indices.Add((short)((z0 + t) * (width + 1) + x0));
                    indices.Add((short)(z0 * (width + 1) + x0 + t));
                    indices.Add((short)((z0 + t) * (width + 1) + x0 + t));
                }

                ibd = new BufferDescription(
                    sizeof(short) * indices.Count,
                    ResourceUsage.Dynamic,
                    BindFlags.IndexBuffer,
                    CpuAccessFlags.Write,
                    ResourceOptionFlags.None,
                    0
                    );
                EdgeIbs[NeighborDir.Top][key] = new Buffer(device, new DataStream(indices.ToArray(), false, false), ibd);
                EdgeIndiceCount[NeighborDir.Top][key] = indices.Count;
            }
            for (var i = 0; i < 6; i++) {
                t = (int)Math.Pow(2, i);
                var t1 = (int)Math.Pow(2, i + 1);
                key = new Tuple<int, int>(t, t1);
                indices = new List<short>();
                z0 = 0;

                
                indices.Add(0);
                indices.Add((short)(t1));
                indices.Add((short)((z0 + t) * (width + 1) + t));

                indices.Add((short)(t1));
                indices.Add((short)(t * (width + 1) + t1));
                indices.Add((short)((t) * (width + 1) + t));
                
                for (int x = 0 + t1, x0 = t1; x < width; x += t1, x0 += t1) {
                    indices.Add((short)(z0 * (width + 1) + x0));
                    indices.Add((short)((z0+t) * (width + 1) + x0 + t));
                    indices.Add((short)((z0 + t) * (width + 1) + x0));

                    indices.Add((short)((z0) * (width + 1) + x0 + t1));
                    indices.Add((short)((z0+t) * (width + 1) + x0 + t1));
                    indices.Add((short)((z0 + t) * (width + 1) + x0 + t));

                    indices.Add((short)((z0) * (width + 1) + x0 ));
                    indices.Add((short)((z0) * (width + 1) + x0 + t1));
                    indices.Add((short)((z0 + t) * (width + 1) + x0+t));
                }
                

                ibd = new BufferDescription(
                    sizeof(short) * indices.Count,
                    ResourceUsage.Dynamic,
                    BindFlags.IndexBuffer,
                    CpuAccessFlags.Write,
                    ResourceOptionFlags.None,
                    0
                    );
                EdgeIbs[NeighborDir.Top][key] = new Buffer(device, new DataStream(indices.ToArray(), false, false), ibd);
                EdgeIndiceCount[NeighborDir.Top][key] = indices.Count;
            }

            for (var i = 1; i <= 6; i++) {
                t = (int)Math.Pow(2, i);
                var t1 = (int)Math.Pow(2, i - 1);
                key = new Tuple<int, int>(t, t1);
                indices = new List<short>();
                z0 = 0;

                
                for (int x = 0 , x0 = 0; x <width - t1; x += t, x0 += t) {
                    indices.Add((short)(z0 * (width + 1) + x0));
                    indices.Add((short)((z0) * (width + 1) + x0 + t1));
                    indices.Add((short)((z0 + t) * (width + 1) + x0));

                    indices.Add((short)((z0) * (width + 1) + x0 + t1));
                    indices.Add((short)((z0) * (width + 1) + x0 + t));
                    indices.Add((short)((z0 + t) * (width + 1) + x0 + t));

                    indices.Add((short)((z0) * (width + 1) + x0 + t1));
                    indices.Add((short)((z0 + t) * (width + 1) + x0 + t));
                    indices.Add((short)((z0 + t) * (width + 1) + x0));
                }

                ibd = new BufferDescription(
                    sizeof(short) * indices.Count,
                    ResourceUsage.Dynamic,
                    BindFlags.IndexBuffer,
                    CpuAccessFlags.Write,
                    ResourceOptionFlags.None,
                    0
                    );
                EdgeIbs[NeighborDir.Top][key] = new Buffer(device, new DataStream(indices.ToArray(), false, false), ibd);
                EdgeIndiceCount[NeighborDir.Top][key] = indices.Count;
            }
        }


        public void Draw(DeviceContext dc, Vector3 camPos, Dictionary<NeighborDir, Patch> neighbors) {
            var tessLevel = TessFactor(camPos);
            dc.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vb, TerrainCP.Stride, 0));

            dc.InputAssembler.SetIndexBuffer(CenterIB[tessLevel], Format.R16_UInt, 0);

            dc.DrawIndexed(CenterIndexCount[tessLevel], 0, 0);

            var topEdge = neighbors[NeighborDir.Top] != null ? neighbors[NeighborDir.Top].TessFactor(camPos) : tessLevel;
            var key = new Tuple<int, int>(tessLevel, topEdge);
            if (EdgeIbs[NeighborDir.Top].ContainsKey(key)) {
                dc.InputAssembler.SetIndexBuffer(EdgeIbs[NeighborDir.Top][key], Format.R16_UInt, 0);
                dc.DrawIndexed(EdgeIndiceCount[NeighborDir.Top][key], 0, 0);
            }
            var leftEdge = neighbors[NeighborDir.Left] != null ? neighbors[NeighborDir.Left].TessFactor(camPos) : tessLevel;
            key = new Tuple<int, int>(tessLevel, leftEdge);
            if (EdgeIbs[NeighborDir.Left].ContainsKey(key)) {
                dc.InputAssembler.SetIndexBuffer(EdgeIbs[NeighborDir.Left][key], Format.R16_UInt, 0);
                dc.DrawIndexed(EdgeIndiceCount[NeighborDir.Left][key], 0, 0);
            }

        }
        [Obsolete("Use Draw(DeviceContext, Vector3, Dictionary<NeighborDir, Patch> instead, unless you want T-junctions")]
        public void Draw(DeviceContext dc, Vector3 camPos ) {
            if (IB.Count == 0) {
                BuildIndices(dc.Device);
            }
            var tessLevel = TessFactor(camPos);
            dc.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(_vb, TerrainCP.Stride, 0));

            dc.InputAssembler.SetIndexBuffer(IB[tessLevel], Format.R16_UInt, 0);

            dc.DrawIndexed(IndexCount[tessLevel], 0, 0);
        }
    }


}