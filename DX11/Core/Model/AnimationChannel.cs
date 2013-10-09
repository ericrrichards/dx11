namespace Core.Model {
    using System.Collections.Generic;

    using Assimp;

    public class AnimationChannel {
        public string Name { get; set; }
        public List<VectorKey> PositionKeys { get; set; }
        public List<QuaternionKey> RotationKeys { get; set; }
        public List<VectorKey> ScalingKeys { get; set; }
    }
}