using System.Collections.Generic;
using System.Linq;

namespace VoronoiMap {
    public enum Criterion {
        Vertex, Site
    }

    internal class EdgeReorderer {
        public List<Edge> Edges { get; private set; }
        public List<Side> EdgeOrientations { get; private set; }

        public EdgeReorderer(List<Edge> origEdges, Criterion c) {
            EdgeOrientations = new List<Side>();
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
            EdgeOrientations.Add(Side.Left);

            Site firstPoint, lastPoint;
            if (!GetPoints(edge, c, out firstPoint, out lastPoint)) {
                return;
            }
            

            done[0] = true;
            var nDone = 1;
            while (nDone < n) {
                for (int i = 1; i < n; i++) {
                    if (done[i]) continue;

                    edge = edges[i];
                    Site leftPoint, rightPoint;
                    if (!GetPoints(edge, c, out leftPoint, out rightPoint)) return;
                    if (leftPoint == rightPoint) {
                        n--;
                        edges.RemoveAt(i);
                        done.RemoveAt(i);
                        break;
                    }

                    if (leftPoint == lastPoint) {
                        lastPoint = rightPoint;
                        EdgeOrientations.Add(Side.Left);
                        newEdges.Add(edge);
                        done[i] = true;
                    } else if (rightPoint == firstPoint) {
                        firstPoint = leftPoint;
                        EdgeOrientations.Insert(0, Side.Left);
                        newEdges.Insert(0, edge);
                        done[i] = true;
                    } else if (leftPoint == firstPoint) {
                        firstPoint = rightPoint;
                        EdgeOrientations.Insert(0, Side.Right);
                        newEdges.Insert(0, edge);
                        done[i] = true;
                    } else if (rightPoint == lastPoint) {
                        lastPoint = leftPoint;
                        EdgeOrientations.Add(Side.Right);
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

        private static bool GetPoints(Edge edge, Criterion c, out Site l, out Site r) {
            l = (c == Criterion.Vertex) ? edge.LeftVertex : edge.LeftSite;
            r = (c == Criterion.Vertex) ? edge.RightVertex : edge.RightSite;
            return true;
        }

    }
}