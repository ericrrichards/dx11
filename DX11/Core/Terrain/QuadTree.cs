using System.Collections.Generic;

namespace Core.Terrain {
    using SlimDX;

    using Ray = SlimDX.Ray;

    public class QuadTree {
        public QuadTreeNode Root;

public bool Intersects(Ray ray, out Vector3 hit, out QuadTreeNode node) {
    return Root.Intersects(ray, out hit, out node);
}
    }

public class QuadTreeNode {
    public BoundingBox Bounds;
    public QuadTreeNode[] Children;
    public MapTile MapTile { get; set; }

public bool Intersects(Ray ray, out Vector3 hit, out QuadTreeNode node) {
    hit = new Vector3(float.MaxValue);


    // This is our terminating condition
    if (Children == null) {
        float d;
        // check if the ray intersects this leaf node's bounding box
        if (!Ray.Intersects(ray, Bounds, out d)) {
            // No intersection
            node = null;
            return false;
        }
        // return the centerpoint of the leaf's bounding box
        hit = (Bounds.Minimum + Bounds.Maximum) / 2;
        node = this;
        return true;
    }

    // If the node has children, we need to intersect each child.
    // We only intersect the child's immediate bounding volume, in order to avoid fully intersecting 
    // It is possible that the closest child intersection does not actually contain the closest
    // node that intersects the ray, so we maintain a priority queue of the child nodes that were hit, 
    // indexed by the distance to intersection
    var pq = new SortedDictionary<float, QuadTreeNode>();
    foreach (var bvhNode in Children) {
        float cd;
        if (Ray.Intersects(ray, bvhNode.Bounds, out cd)) {
            while (pq.ContainsKey(cd)) {
                // perturb things slightly so that we don't have duplicate keys
                cd += MathF.Rand(-0.001f, 0.001f);
            }
            pq.Add(cd, bvhNode);
        }
    }

    // If there were no child intersections
    if (pq.Count <= 0) {
        node = null;
        return false;
    }

    // check the child intersections for the nearest intersection
    var intersect = false;
    // setup a very-far away intersection point to compare against
    var bestHit = ray.Position + ray.Direction * 1000;
    QuadTreeNode bestNode = null;
    foreach (var bvhNode in pq) {
        Vector3 thisHit;
        QuadTreeNode thisNode;
        // intersect the child node recursively
        var wasHit = bvhNode.Value.Intersects(ray, out thisHit, out thisNode);
        if (!wasHit) {
            // no intersection, continue and intersect the other children
            continue;
        }
        // Make sure that the intersection point is in front of the ray's world-space origin
        var dot = (Vector3.Dot(Vector3.Normalize(thisHit - ray.Position), ray.Direction));
        if (!(dot > 0.9f)) {
            continue;
        }

        // check that the intersection is closer than the nearest intersection found thus far
        if (!((ray.Position - thisHit).LengthSquared() < (ray.Position - bestHit).LengthSquared()))
            continue;

        // if we have found a closer intersection store the new closest intersection
        bestHit = thisHit;
        bestNode = thisNode;
        intersect = true;
    }
    // bestHit now contains the closest intersection found, or the distant sentinel value
    hit = bestHit;
    node = bestNode;
    return intersect;
}
    }
}
