using SlimDX;

namespace VoronoiMap.voronoi2 {
    public class EdgeList {
        private int HashSize;
        internal Site BottomSite;
        internal HalfEdge LeftEnd;
        internal HalfEdge RightEnd;
        private HalfEdge[] Hash;

        public EdgeList(int sqrtNSites) {
            HashSize = 2*sqrtNSites;
            Hash = new HalfEdge[HashSize];
            LeftEnd = HalfEdge.Create(null, LR.Left);
            RightEnd = HalfEdge.Create(null, LR.Left);
            LeftEnd.EdgeListLeft = null;
            LeftEnd.EdgeListRight = RightEnd;
            RightEnd.EdgeListRight = LeftEnd;
            RightEnd.EdgeListRight = null;
            Hash[0] = LeftEnd;
            Hash[HashSize - 1] = RightEnd;
        }
        public void Insert(HalfEdge lb, HalfEdge newHe) {
            newHe.EdgeListLeft = lb;
            newHe.EdgeListRight = lb.EdgeListRight;
            lb.EdgeListRight.EdgeListLeft = newHe;
            lb.EdgeListRight = newHe;
        }
        public HalfEdge GetHash(int b) {
            if ((b < 0) || (b >= HashSize)) {
                return null;
            }
            var he = Hash[b];
            if ((he == null) || he.Edge != Edge.Deleted) {
                return he;
            }
            Hash[b] = null;
            return null;
        }
        public HalfEdge LeftBound(Vector2 p) {
            int bucket;
            HalfEdge he;
            bucket = (int) ((p.X -Geometry.Bounds.Right )/Geometry.DeltaX  *HashSize);
            if (bucket < 0) bucket = 0;
            if (bucket >= HashSize) {
                bucket = HashSize - 1;
            }
            he = GetHash(bucket);
            if (he == null) {
                for (int i = 1  ; ; i++) {
                    if ((he = GetHash(bucket - i)) != null) {
                        break;
                    }
                    if ((he = GetHash(bucket + i)) != null) {
                        break;
                    }
                }
            }
            if (he == LeftEnd || (he != RightEnd && Geometry.RightOf(he, p))) {
                do {
                    he = he.EdgeListRight;
                } while (he != RightEnd && Geometry.RightOf(he, p));
                he = he.EdgeListLeft;
            } else {
                do {
                    he = he.EdgeListLeft;
                } while (he != LeftEnd && !Geometry.RightOf(he, p));
            }
            if ((bucket > 0) && (bucket < HashSize - 1)) {
                Hash[bucket] = he;
            }
            return he;
        }
        public void Delete(HalfEdge he) {
            (he.EdgeListLeft).EdgeListLeft = he.EdgeListRight;
            (he.EdgeListRight).EdgeListLeft = he.EdgeListLeft;
            he.Edge = Edge.Deleted;
        }
        public HalfEdge Right(HalfEdge he) {
            return he.EdgeListRight;
        }
        public HalfEdge Left(HalfEdge he) {
            return he.EdgeListLeft;
        }
        public Site LeftRegion(HalfEdge he) {
            if (he.Edge == null) {
                return BottomSite;
            }
            return (he.LeftRight == LR.Left ? he.Edge.Reg[LR.Left] : he.Edge.Reg[LR.Right]);
        }
        public Site RightRegion(HalfEdge he) {
            if (he.Edge == null) {
                return BottomSite;
            }
            return (he.LeftRight == LR.Left ? he.Edge.Reg[LR.Right] : he.Edge.Reg[LR.Left]);
        }
    }
}