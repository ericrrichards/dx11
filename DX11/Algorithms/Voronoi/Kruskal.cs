using System;
using System.Collections.Generic;

namespace Algorithms.Voronoi {
    public class Kruskal {
        public static List<LineSegment> KruskalTree(List<LineSegment> segments, KruskalType type= KruskalType.Minimum) {
            var nodes = new Dictionary<Point, Node>();
            var mst = new List<LineSegment>();
            switch (type) {
                // note that the compare functions are the reverse of what you'd expect
                // because (see below) we traverse the lineSegments in reverse order for speed
                case KruskalType.Maximum:
                    segments.Sort(LineSegment.CompareLengths);
                    break;
                default:
                    segments.Sort(LineSegment.CompareLengthsMax);
                    break;
            }
            for (int i = segments.Count; --i > -1; ) {
                var segment = segments[i];
                Node rootOfSet0 = null;
                Node node0;
                if (!nodes.ContainsKey(segment.P0)) {
                    node0 = new Node();
                    // intialize the node:
                    rootOfSet0 = node0.Parent = node0;
                    node0.TreeSize = 1;

                    nodes[segment.P0] = node0;
                } else {
                    node0 = nodes[segment.P0];
                    rootOfSet0 = Node.Find(node0);
                }
                Node node1;
                Node rootOfSet1;
                if (!nodes.ContainsKey(segment.P1)) {
                    node1 = new Node();
                    // intialize the node:
                    rootOfSet1 = node1.Parent = node1;
                    node1.TreeSize = 1;
                    nodes[segment.P1] = node1;
                } else {
                    node1 = nodes[segment.P1];
                    rootOfSet1 = Node.Find(node1);
                }

                if (rootOfSet0 != rootOfSet1) { // nodes not in same set
                    mst.Add(segment);

                    // merge the two sets:
                    var treeSize0 = rootOfSet0.TreeSize;
                    var treeSize1 = rootOfSet1.TreeSize;
                    if (treeSize0 >= treeSize1) {
                        // set0 absorbs set1:
                        rootOfSet1.Parent = rootOfSet0;
                        rootOfSet0.TreeSize += treeSize1;
                    } else {
                        // set1 absorbs set0:
                        rootOfSet0.Parent = rootOfSet1;
                        rootOfSet1.TreeSize += treeSize0;
                    }
                }
            }
            return mst;
        }

        public class Node {
            public Node Parent { get; set; }
            public int TreeSize { get; set; }

            public static Node Find(Node node) {
                if (node.Parent == node) {
                    return node;
                } else {
                    var root = Find(node.Parent);
                    // this line is just to speed up subsequent finds by keeping the tree depth low:
                    node.Parent = root;
                    return root;
                }
            }
        }
    }
}