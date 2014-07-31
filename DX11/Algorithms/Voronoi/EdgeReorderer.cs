using System.Collections.Generic;
using System.Linq;

namespace Algorithms.Voronoi {
    public enum Criterion {
        cVertex, cSite
    }

    internal class EdgeReorderer {
        public List<Edge> Edges { get; private set; }
        public List<LR.Side> EdgeOrientations { get; private set; }

        public EdgeReorderer(List<Edge> origEdges, Criterion c) {
            EdgeOrientations = new List<LR.Side>();
            Edges = new List<Edge>();

            if (origEdges.Count > 0) {
                ReorderEdges(origEdges, c);
            }
        }

        private void ReorderEdges(List<Edge> edges, Criterion c) {
            var n = edges.Count;
            var done = new List<bool>();
            for (int i = 0; i < n; i++) {
                done.Add(false);
            }
            var newEdges = new List<Edge>();
            var edge = edges.First();
            newEdges.Add(edge);
            EdgeOrientations.Add(LR.Side.Left);

            ICoord firstPoint, lastPoint;
            if (!GetPoints(edge, c, out firstPoint, out lastPoint)) {
                return;
            }
            if (firstPoint == Vertex.VertexAtInfinity || lastPoint == Vertex.VertexAtInfinity) {
                return;
            }

            done[0] = true;
            var nDone = 1;
            while (nDone < n) {
                for (int i = 1; i < n; i++) {
                    if ( done[i]) continue;

                    edge = edges[i];
                    ICoord leftPoint, rightPoint;
                    if (!GetPoints(edge, c, out leftPoint, out rightPoint)) return;

                    if (leftPoint == lastPoint) {
                        lastPoint = rightPoint;
                        EdgeOrientations.Add(LR.Side.Left);
                        newEdges.Add(edge);
                        done[i] = true;
                    } else if (rightPoint == firstPoint) {
                        firstPoint = leftPoint;
                        EdgeOrientations.Insert(0, LR.Side.Left);
                        newEdges.Insert(0, edge);
                        done[i] = true;
                    } else if (leftPoint == firstPoint) {
                        firstPoint = rightPoint;
                        EdgeOrientations.Insert(0, LR.Side.Right);
                        newEdges.Insert(0, edge);
                        done[i] = true;
                    } else if (rightPoint == lastPoint) {
                        lastPoint = leftPoint;
                        EdgeOrientations.Add(LR.Side.Right);
                        newEdges.Add(edge);
                        done[i] = true;
                    }
                    if (done[i]) {
                        ++nDone;
                    }
                }
            }
            Edges = newEdges;
        }

        private static bool GetPoints(Edge edge, Criterion c, out ICoord l, out ICoord r) {
            l = (c == Criterion.cVertex) ? edge.LeftVertex : edge.LeftSite as ICoord;
            r = (c == Criterion.cVertex) ? edge.RightVertex : edge.RightSite as ICoord;
            return l != Vertex.VertexAtInfinity && r != Vertex.VertexAtInfinity;
        }
        
    }
}