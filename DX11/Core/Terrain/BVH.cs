using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Terrain {
    using SlimDX;

    class BVH {
        public BVHNode Root;

        public bool Intersects(Ray ray, out Vector3 hit) {
            return Root.Intersects(ray, out hit);
        }
    }

    class BVHNode {
        public BoundingBox Bounds;
        public BVHNode[] Children = new BVHNode[4];

        public bool Intersects(Ray ray, out Vector3 hit) {
            /*var p = new Plane(new Vector3(0, 1, 0), 0);

            float planeD;
            Vector3 planeHit;
            if (Ray.Intersects(ray, p, out planeD)) {
                planeHit = ray.Position + ray.Direction*planeD;
            }
            */


            var intersects = false;
            float d;
            if (Ray.Intersects(ray, Bounds, out d)) {
                if (Children.All(c => c == null)) {
                    hit =  (Bounds.Minimum + Bounds.Maximum ) /2;
                    return true;
                }

                var pq = new SortedDictionary<float, BVHNode>();
                foreach (var bvhNode in Children) {
                    float cd;
                    if (Ray.Intersects(ray, bvhNode.Bounds, out cd)) {
                        pq.Add(cd, bvhNode);
                    }
                }


                if (pq.Count> 0) {
                    var intersect = false;
                    var bestHit = ray.Position + ray.Direction*1000;
                    foreach (var bvhNode in pq) {
                        Vector3 thisHit;
                        var i = bvhNode.Value.Intersects(ray, out thisHit);
                        if (i ) {
                            var dot = (Vector3.Dot(Vector3.Normalize(thisHit - ray.Position), ray.Direction));
                            if (dot > 0.8f) {
                                if ((ray.Position - thisHit).LengthSquared() < (ray.Position - bestHit).LengthSquared()) {
                                    bestHit = thisHit;
                                    intersect = true;
                                }
                            }

                        }
                    }
                    hit = bestHit;
                    intersects = intersect;
                } else {
                    intersects = false;
                    hit = new Vector3(float.MaxValue);
                }


            } else {
                hit = new Vector3(float.MaxValue);
            }

            return intersects;
        }
    }
}
