using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Terrain {
    using SlimDX;

    class BVH {
        public BVHNode Root;
    }

    class BVHNode {
        public BoundingBox Bounds;
        public BVHNode[] Children = new BVHNode[4];
    }
}
