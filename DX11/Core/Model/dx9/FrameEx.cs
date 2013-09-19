using SlimDX;
using SlimDX.Direct3D9;

namespace Core.Model.dx9 {
    public class FrameEx : Frame {
        public Matrix ToRoot;
        public FrameEx() {
            TransformationMatrix = Matrix.Identity;
            ToRoot = Matrix.Identity;
        }
    }
}