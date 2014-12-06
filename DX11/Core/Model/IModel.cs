using System.Collections.Generic;
using SlimDX;
using SlimDX.Direct3D11;

namespace Core.Model {
    using System;
    using System.Windows.Forms;

    public abstract class IModel<TVertexType> : DisposableClass {
        public MeshGeometry ModelMesh { get; protected set; }
        protected List<MeshGeometry.Subset> Subsets;
        public int SubsetCount { get { return Subsets.Count; } }
        protected List<int> Indices;
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
                    Indices.Clear();
                    Vertices.Clear();
                    Indices = null;
                    Vertices = null;
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        protected IModel() {
            Subsets = new List<MeshGeometry.Subset>();
            Vertices = new List<TVertexType>();
            Indices = new List<int>();
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
        public abstract void CreateGeosphere(Device device, float radius, GeometryGenerator.SubdivisionCount subdivisionCount);
    }

    public enum RenderMode {
        NormalMapped,
        Basic,
        DisplacementMapped,
        ShadowMap,
        NormalDepthMap
    }

    public abstract class IModelInstance<T> {
        public IModel<T> Model { get; set; }
        public Matrix World { get; set; }
        public Matrix ShadowTransform { get; set; }
        public Matrix TexTransform { get; set; }
        public Matrix ToTexSpace { get; set; }
        public BoundingBox BoundingBox { get { return new BoundingBox(Vector3.TransformCoordinate(Model.BoundingBox.Minimum, World), Vector3.TransformCoordinate(Model.BoundingBox.Maximum, World)); } }

        protected IModelInstance() {
            World = Matrix.Identity;
            ShadowTransform = Matrix.Identity;
            TexTransform = Matrix.Identity;
            ToTexSpace = Matrix.Identity;
        }

        protected IModelInstance(IModel<T> model)
            : this() {
            Model = model;
        }

        public void Draw(DeviceContext dc, EffectPass effectPass, Matrix view, Matrix proj, ModelDrawDelegate method) {
            method(dc, effectPass, view, proj);
        }

        public void Draw(DeviceContext dc, EffectPass effectPass, Matrix view, Matrix proj, RenderMode renderMode = RenderMode.NormalMapped) {
            switch (renderMode) {
                case RenderMode.NormalMapped:
                    DrawNormalMapped(dc, effectPass, view * proj);
                    break;
                case RenderMode.Basic:
                    DrawBasic(dc, effectPass, view * proj);
                    break;
                case RenderMode.DisplacementMapped:
                    DrawDisplacementMapped(dc, effectPass, view * proj);
                    break;
                case RenderMode.ShadowMap:
                    DrawShadowMap(dc, effectPass, view * proj);
                    break;
                case RenderMode.NormalDepthMap:
                    DrawNormalDepth(dc, effectPass, view, proj);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("renderMode");
            }
        }

        protected abstract void DrawNormalDepth(DeviceContext dc, EffectPass effectPass, Matrix view, Matrix proj);
        protected abstract void DrawShadowMap(DeviceContext dc, EffectPass effectPass, Matrix viewProj);
        protected abstract void DrawDisplacementMapped(DeviceContext dc, EffectPass effectPass, Matrix viewProj);
        protected abstract void DrawBasic(DeviceContext dc, EffectPass effectPass, Matrix viewProj);
        protected abstract void DrawNormalMapped(DeviceContext dc, EffectPass effectPass, Matrix viewProj);
    }

    public delegate void ModelDrawDelegate(DeviceContext dc, EffectPass pass, Matrix view, Matrix proj);
}
