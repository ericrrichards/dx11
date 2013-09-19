using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SlimDX;
using SlimDX.Direct3D9;

namespace Core.Model.dx9 {
    public class SkinnedMesh {
        public long NumVertices { get; private set; }
        public long NumTriangles { get; private set; }
        public long NumBones { get; private set; }

        public CustomMeshContainer MeshContainer { get { return _meshContainer; } }

        public Frame Root { get { return _root; } }

        public AnimationController AnimationController { get { return _animationController; } }

        public Dictionary<string, int> Animations { get { return _animations; } }

        private readonly Frame _root;
        private readonly AnimationController _animationController;
        private readonly Dictionary<string, int> _animations = new Dictionary<string, int>();
        private static readonly Device sDevice;
        private CustomMeshContainer _meshContainer;

        static SkinnedMesh() {
            var d3d = new Direct3D();
            try {
                var mode = d3d.GetAdapterDisplayMode(0);
                var pp = new PresentParameters() {
                    BackBufferWidth = 1,
                    BackBufferHeight = 1,
                    BackBufferFormat = mode.Format,
                    BackBufferCount = 1,
                    SwapEffect = SwapEffect.Copy,
                    Windowed = true,
                    DeviceWindowHandle = D3DApp.GD3DApp.Window.Handle
                };

                sDevice = new Device(d3d, 0, DeviceType.Reference, D3DApp.GD3DApp.Window.Handle, CreateFlags.HardwareVertexProcessing,pp);
            } catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }

        public SkinnedMesh(string meshFile) {
            _root = Frame.LoadHierarchyFromX(sDevice, meshFile, MeshFlags.Managed, new AllocMeshHierarchy(), null, out _animationController);
            SetupBoneMatrices(_root);
            GetMeshInfo(_root);

            GetAnimationIndices();

            

        }
        private void GetAnimationIndices() {
            for (int i = 0; i < _animationController.AnimationSetCount; i++) {
                var anim = _animationController.GetAnimationSet<AnimationSet>(i);
                _animations[anim.Name] = i;
                anim.Dispose();
            }
        }
        private void GetMeshInfo(Frame root) {
            if (root.MeshContainer != null) {
                _meshContainer = root.MeshContainer as CustomMeshContainer;
                NumBones = root.MeshContainer.SkinInfo.BoneCount;
                NumVertices = root.MeshContainer.MeshData.Mesh.VertexCount;
                NumTriangles = root.MeshContainer.MeshData.Mesh.FaceCount;
                return;
            }
            if (root.Sibling != null) {
                GetMeshInfo(root.Sibling);
            }
            if (root.FirstChild != null) {
                GetMeshInfo(root.FirstChild);
            }
        }
        private void SetupBoneMatrices(Frame frame) {
            if (frame.MeshContainer != null) {
                SetupBoneMatrices(frame.MeshContainer as CustomMeshContainer);
            }
            if (frame.Sibling != null) {
                SetupBoneMatrices(frame.Sibling);
            }
            if (frame.FirstChild != null) {
                SetupBoneMatrices(frame.FirstChild);
            }
        }
        private void SetupBoneMatrices(CustomMeshContainer meshContainer) {
            if (meshContainer.SkinInfo == null) {
                return;
            }
            meshContainer.BoneMatricesLookup = new FrameEx[meshContainer.SkinInfo.BoneCount];
            for (int i = 0; i < meshContainer.SkinInfo.BoneCount; i++) {
                var frame = (FrameEx)_root.FindChild(meshContainer.SkinInfo.GetBoneName(i));
                meshContainer.BoneMatricesLookup[i] = frame;
            }
        }
    }
}
