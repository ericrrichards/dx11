using System.Collections.Generic;

namespace Algorithms.Voronoi {
    internal class EdgeList {
        private float _deltax;
        private float _xmin;
        private int _hashsize;
        private List<HalfEdge> _hash;
        
        public HalfEdge LeftEnd { get; private set; }
        public HalfEdge RightEnd { get; private set; }

        public EdgeList(float xmin, float deltax, int sqrtNSites) {
            _xmin = xmin;
            _deltax = deltax;
            _hashsize = 2*sqrtNSites;

            _hash = new List<HalfEdge>(_hashsize);
            for (int i = 0; i < _hashsize; i++) {
                _hash.Add(null);
            }
            LeftEnd = HalfEdge.CreateDummy();
            RightEnd = HalfEdge.CreateDummy();
            LeftEnd.EdgeListLeftNeighbor = null;
            LeftEnd.EdgeListRightNeighbor = RightEnd;
            RightEnd.EdgeListLeftNeighbor = LeftEnd;
            RightEnd.EdgeListRightNeighbor = null;
            _hash[0] = LeftEnd;
            _hash[_hashsize-1] = RightEnd;
        }

        public void Insert(HalfEdge lb, HalfEdge he) {
            he.EdgeListLeftNeighbor = lb;
            he.EdgeListRightNeighbor = lb.EdgeListRightNeighbor;
            lb.EdgeListRightNeighbor.EdgeListLeftNeighbor = he;
            lb.EdgeListRightNeighbor = he;
        }

        public void Remove(HalfEdge he) {
            he.EdgeListLeftNeighbor.EdgeListRightNeighbor = he.EdgeListRightNeighbor;
            he.EdgeListRightNeighbor.EdgeListLeftNeighbor = he.EdgeListLeftNeighbor;
            he.Edge = Edge.DELETED;
            he.EdgeListLeftNeighbor = he.EdgeListRightNeighbor = null;
        }

        public HalfEdge EdgeListLeftNeighbor(Point p) {
            var bucket = (int) ((p.X - _xmin)/_deltax*_hashsize);
            if (bucket < 0) bucket = 0;
            if (bucket >= _hashsize) bucket = _hashsize - 1;

            var he = GetHash(bucket);
            if (he == null) {
                for (int i = 1; true ; i++) {
                    if ((he = GetHash(bucket - i)) != null) break;
                    if ((he=GetHash(bucket+i))!=null) break;
                }
            }
            if (he == LeftEnd || (he != RightEnd && he.IsLeftOf(p))) {
                do {
                    he = he.EdgeListRightNeighbor;
                } while (he != RightEnd && he.IsLeftOf(p));
                he = he.EdgeListLeftNeighbor;
            } else {
                do {
                    he = he.EdgeListLeftNeighbor;
                } while (he != LeftEnd && !he.IsLeftOf(p));
            }
            if (bucket > 0 && bucket < _hashsize - 1) {
                _hash[bucket] = he;
            }
            return he;
        }

        public HalfEdge GetHash(int b) {
            if (b < 0 || b >= _hashsize) {
                return null;
            }
            var he = _hash[b];
            if (he != null && he.Edge == Edge.DELETED) {
                _hash[b] = null;
                return null;
            }
            return he;
        }
    }
}