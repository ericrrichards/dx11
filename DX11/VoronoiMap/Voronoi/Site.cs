using System;
using System.Collections.Generic;
using System.Linq;
using SlimDX;

namespace VoronoiMap.Voronoi {
    public class Site : ICoord {

        private const float Epsilon = 0.005f;
        private Color4 _color;
        private float _weight;
        private int _siteIndex;
        public List<Edge> Edges { get; private set; };
        private List<LR> _edgeOrientations;
        private List<Vector2> _region; 

        public Site(Vector2 p, int index, float weight, Color4 color) {
            Init(p, index, weight, color);
        }

        private void Init(Vector2 p, int index, float weight, Color4 color) {
            Coord = p;
            _siteIndex = index;
            _weight = weight;
            _color = color;
            Edges = new List<Edge>();
            _region = null;
        }

        

        public class SiteComparer : IComparer<Site> {
            public int Compare(Site s1, Site s2) {
                var ret = Voronoi.CompareByYThenX(s1, s2);
                if (ret == -1) {
                    if (s1._siteIndex > s2._siteIndex) {
                        var tempIndex = s1._siteIndex;
                        s1._siteIndex = s2._siteIndex;
                        s2._siteIndex = tempIndex;
                    } else if (ret == 1) {
                        var tempIndex = s2._siteIndex;
                        s2._siteIndex = s1._siteIndex;
                        s1._siteIndex = tempIndex;
                    }
                }
                return ret;
            }
        }

        public void AddEdge(Edge edge) {
            Edges.Add(edge);
        }

        public float Distance(ICoord p) {
            return Vector2.Distance(p.Coord, Coord);
        }

        public List<Vector2> Region(Rectangle clipBounds) {
            if (Edges == null || Edges.Count == 0) {
                return new List<Vector2>();
            }
            if (_edgeOrientations == null) {
                ReorderEdges();
                _region = ClipToBounds(clipBounds);
                if ((new Polygon(_region)).Winding == Winding.Clockwise) {
                    _region.Reverse();
                }
            }
            return _region;
        }

        private void ReorderEdges() {
            var reorderer = new EdgeReorderer(Edges, typeof(Vertex));
            Edges = reorderer.Edges;
            _edgeOrientations = reorderer.EdgeOrientations;

        }
    }

    class EdgeReorderer {
        private List<Edge> _edges;
        private List<LR> _edgeOrientations;
        public List<Edge> Edges { get { return _edges; } }
        public List<LR> EdgeOrientations { get { return _edgeOrientations; } }

        public EdgeReorderer(List<Edge> origEdges, Type criterion) {
            if (criterion != typeof (Vertex) && criterion != typeof (Site)) {
                throw new ArgumentException("Edges: criterion must e Vertex or Site");
            }
            _edges = new List<Edge>();
            _edgeOrientations = new List<LR>();
            if (origEdges.Count > 0) {
                _edges = reorderEdges(origEdges, criterion);
            }

        }

        private List<Edge> reorderEdges(List<Edge> origEdges, Type criterion) {
            var n = origEdges.Count;
            var done = Enumerable.Repeat(false, n).ToArray();
            var nDone = 0;
            var newEdges = new List<Edge>();

            var i = 0;
            var edge = origEdges[i];
            newEdges.Add(edge);
            _edgeOrientations.Add(LR.Left);
            ICoord firstPoint = (criterion == typeof (Vertex) ? (ICoord) edge.LeftVertex : edge.LeftSite);
            ICoord lastPoint = (criterion == typeof (Vertex) ? (ICoord) edge.RightVertex : edge.RightSite);

            if (firstPoint == Vertex.VertexAtInfinity || lastPoint == Vertex.VertexAtInfinity) {
                return new List<Edge>();
            }
            done[i] = true;
            ++nDone;

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
                        _edgeOrientations.Add(LR.Left);
                        newEdges.Add(edge);
                        done[i] = true;
                    } else  if (rightPoint == firstPoint) {
                        firstPoint = leftPoint;
                        _edgeOrientations.Add(LR.Left);
                        newEdges.Insert(0, edge);
                        done[i] = true;
                    } else if (leftPoint == firstPoint) {
                        firstPoint = rightPoint;
                        _edgeOrientations.Insert(0, LR.Right);
                        newEdges.Insert(0, edge);
                        done[i] = true;
                    } else if (rightPoint == lastPoint) {
                        lastPoint = leftPoint;
                        _edgeOrientations.Add(LR.Right);
                        newEdges.Add(edge);
                        done[i] = true;
                    }
                    if (done[i]) {
                        ++nDone;
                    }
                }
            }
            return newEdges;
        }
    }
}