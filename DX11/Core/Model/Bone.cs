namespace Core.Model {
    using System.Collections.Generic;

    using SlimDX;

    public class Bone {
        public string Name { get; set; }
        // Bind space transform
        public Matrix Offset { get; set; }
        // local matrix transform
        public Matrix LocalTransform { get; set; }
        // To-root transform
        public Matrix GlobalTransform { get; set; }
        // copy of the original local transform
        public Matrix OriginalLocalTransform { get; set; }
        // parent bone reference
        public Bone Parent { get; set; }
        // child bone references
        public List<Bone> Children { get; private set; }
        public Bone() {
            Children = new List<Bone>();
        }
    }
}