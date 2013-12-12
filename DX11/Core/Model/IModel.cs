using System.Collections.Generic;
using SlimDX;
using SlimDX.Direct3D11;

namespace Core.Model {
    public abstract class IModel<TVertexType> : DisposableClass {
        public MeshGeometry ModelMesh { get; protected set; }
        protected List<MeshGeometry.Subset> Subsets;
        public int SubsetCount { get { return Subsets.Count; } }
        protected List<short> Indices;
        protected List<TVertexType> Vertices;
        public List<Material> Materials { get; protected set; }
        public List<ShaderResourceView> DiffuseMapSRV { get; protected set; }
        public List<ShaderResourceView> NormalMapSRV { get; protected set; }

        public BoundingBox BoundingBox { get; protected set; }

        private bool _disposed;

        protected override void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    var meshGeometry = ModelMesh;
                    Util.ReleaseCom(ref meshGeometry);
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        protected IModel() {
            Subsets = new List<MeshGeometry.Subset>();
            Vertices = new List<TVertexType>();
            Indices = new List<short>();
            DiffuseMapSRV = new List<ShaderResourceView>();
            NormalMapSRV = new List<ShaderResourceView>();
            Materials = new List<Material>();
            ModelMesh = new MeshGeometry();
        }

        protected abstract void InitFromMeshData(Device device, GeometryGenerator.MeshData mesh);

        public abstract void CreateBox(Device device, float width, float height, float depth);
        public abstract void CreateSphere(Device device, float radius, int slices, int stacks);
        public abstract void CreateCylinder(Device device, float bottomRadius, float topRadius, float height, int sliceCount, int stackCount);
        public abstract void CreateGrid(Device device, float width, float depth, int xVerts, int zVerts);
    }
}
