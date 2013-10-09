namespace Core.Model {
    using System.Collections.Generic;

    using SlimDX;

    public class Bone {
        public string Name { get; set; }
        public Matrix Offset { get; set; }
        public Matrix LocalTransform { get; set; }
        public Matrix GlobalTransform { get; set; }
        public Matrix OriginalLocalTransform { get; set; }
        public Bone Parent { get; set; }
        public List<Bone> Children { get; set; }
        public Bone() {
            Children = new List<Bone>();
        }
    }
}