using System;
using System.Collections.Generic;
using System.Linq;

namespace VoronoiMap.Voronoi {
    class EdgeReorderer {
        public List<Edge> Edges { get; private set; }
        public List<LR> EdgeOrientations { get; private set; }

        public EdgeReorderer(List<Edge> origEdges, Type criterion) {
            if (criterion != typeof (Vertex) && criterion != typeof (Site)) {
                throw new ArgumentException("Edges: criterion must be Vertex or Site");
            }
            Edges = new List<Edge>();
            EdgeOrientations = new List<LR>();
            if (origEdges.Count > 0) {
                Edges = ReorderEdges(origEdges, criterion);
            }

        }

        private List<Edge> ReorderEdges(List<Edge> origEdges, Type criterion) {
            var n = origEdges.Count;
            var done = Enumerable.Repeat(false, n).ToArray();
            var nDone = 0;
            var newEdges = new List<Edge>();

            var i = 0;
            var edge = origEdges[i];
            newEdges.Add(edge);
            EdgeOrientations.Add(LR.Left);
            ICoord firstPoint = (criterion == typeof (Vertex) ? (ICoord) edge.LeftVertex : edge.LeftSite);
            ICoord lastPoint = (criterion == typeof (Vertex) ? (ICoord) edge.RightVertex : edge.RightSite);

            if (firstPoint == Vertex.VertexAtInfinity || lastPoint == Vertex.VertexAtInfinity) {
                return new List<Edge>();
            }
            done[i] = true;
            ++nDone;
            var loopCount = 0;
            while (nDone < n) {
                for (i = 1; i < n; i++) {
                    if (done[i]) {
                        continue;
                    }
                    edge = origEdges[i];
                    ICoord leftPoint = (criterion == typeof (Vertex) ? (ICoord) edge.LeftVertex : edge.LeftSite);
                    ICoord rightPoint = (criterion == typeof (Vertex) ? (ICoord) edge.RightVertex : edge.RightSite);
                    if (leftPoint == Vertex.VertexAtInfinity || rightPoint == Vertex.VertexAtInfinity) {
                        return new List<Edge>();
                    }
                    if (leftPoint == lastPoint) {
                        lastPoint = rightPoint;
                        EdgeOrientations.Add(LR.Left);
                        newEdges.Add(edge);
                        done[i] = true;
                    } else  if (rightPoint == firstPoint) {
                        firstPoint = leftPoint;
                        EdgeOrientations.Add(LR.Left);
                        newEdges.Insert(0, edge);
                        done[i] = true;
                    } else if (leftPoint == firstPoint) {
                        firstPoint = rightPoint;
                        EdgeOrientations.Insert(0, LR.Right);
                        newEdges.Insert(0, edge);
                        done[i] = true;
                    } else if (rightPoint == lastPoint) {
                        lastPoint = leftPoint;
                        EdgeOrientations.Add(LR.Right);
                        newEdges.Add(edge);
                        done[i] = true;
                    }
                    if (done[i]) {
                        ++nDone;
                    }
                }
                loopCount++;
                if (loopCount > 1000) 
                    break;
            }
            return newEdges;
        }
    }
}