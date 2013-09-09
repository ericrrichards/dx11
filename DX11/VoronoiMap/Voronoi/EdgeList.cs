using System.Collections.Generic;
using SlimDX;

namespace VoronoiMap.Voronoi {
    class EdgeList {
        private float _xmin;
        private float _deltaX;
        private int _hashSize;
        private List<HalfEdge> _hash;
        public HalfEdge LeftEnd { get; private set; }
        public HalfEdge RightEnd { get; private set; }

        public EdgeList(float xmin, float deltax, int sqrtNSites) {
            _xmin = xmin;
            _deltaX = deltax;
            _hashSize = 2 * sqrtNSites;

            _hash = new List<HalfEdge>(new HalfEdge[_hashSize]);

            LeftEnd = new HalfEdge();
            RightEnd = new HalfEdge();
            LeftEnd.EdgeListLeftNeighbor = null;
            LeftEnd.EdgeListRightNeighbor = RightEnd;
            RightEnd.EdgeListLeftNeighbor = LeftEnd;
            RightEnd.EdgeListRightNeighbor = null;
            _hash[0] = LeftEnd;
            _hash[_hashSize - 1] = RightEnd;
        }

        public HalfEdge EdgeListLeftNeighbor(Vector2 p) {
            var bucket = (int) ((p.X - _xmin)/_deltaX*_hashSize);
            if (bucket < 0) {
                bucket = 0;
            }
            if (bucket >= _hashSize) {
                bucket = _hashSize - 1;
            }
            var halfEdge = GetHash(bucket);
            if (halfEdge == null) {
                for (int i = 1;; i++) {
                    if ((halfEdge = GetHash(bucket - 1)) != null) break;
                    if ((halfEdge = GetHash(bucket + 1)) != null) break;
                }
            }
            if (halfEdge == LeftEnd || (halfEdge != RightEnd && halfEdge.IsLeftOf(p))) {
                do {
                    halfEdge = halfEdge.EdgeListRightNeighbor;

                } while (halfEdge != RightEnd && halfEdge.IsLeftOf(p));
                halfEdge = halfEdge.EdgeListLeftNeighbor;
            } else {
                do {
                    halfEdge = halfEdge.EdgeListLeftNeighbor;
                } while (halfEdge != LeftEnd && !halfEdge.IsLeftOf(p));

            }
            if (bucket > 0 && bucket < _hashSize - 1) {
                _hash[bucket] = halfEdge;
            }
            return halfEdge;
        }

        private HalfEdge GetHash(int b) {
            if (b < 0 || b >= _hashSize) {
                return null;
            }
            var halfEdge = _hash[b];
            if (halfEdge != null && halfEdge.Edge == Edge.Deleted) {
                _hash[b] = null;
                return null;
            }
            return halfEdge;
        }

        public void Insert(HalfEdge lb, HalfEdge newHalfEdge) {
            newHalfEdge.EdgeListLeftNeighbor = lb;
            newHalfEdge.EdgeListRightNeighbor = lb.EdgeListRightNeighbor;
            lb.EdgeListRightNeighbor.EdgeListLeftNeighbor = newHalfEdge;
            lb.EdgeListRightNeighbor = newHalfEdge;
        }

        public void Remove(HalfEdge halfEdge) {
            halfEdge.EdgeListLeftNeighbor.EdgeListRightNeighbor = halfEdge.EdgeListRightNeighbor;
            halfEdge.EdgeListRightNeighbor.EdgeListLeftNeighbor = halfEdge.EdgeListLeftNeighbor;
            halfEdge.Edge = Edge.Deleted;
            halfEdge.EdgeListLeftNeighbor = halfEdge.EdgeListRightNeighbor = null;
        }
    }
}