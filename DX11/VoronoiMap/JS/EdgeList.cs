using System.Collections.Generic;

namespace Fortune.FromJS {
    public class EdgeList {
        public List<HalfEdge> Edges { get; set; }
        public HalfEdge LeftEnd { get; set; }
        public HalfEdge RightEnd { get; set; }

        private readonly SiteList _siteList;

        public EdgeList( SiteList siteList) {
            Edges = new List<HalfEdge>();
            _siteList = siteList;
            LeftEnd = new HalfEdge(null, Side.Left);
            RightEnd = new HalfEdge(null, Side.Left);
            LeftEnd.Right = RightEnd;
            RightEnd.Left = LeftEnd;

            Edges.Add(LeftEnd);
            Edges.Add(RightEnd);
        }

        public void Insert(HalfEdge lb, HalfEdge he) {
            he.Left = lb;
            he.Right = lb.Right;
            lb.Right.Left = he;
            lb.Right = he;
        }

        public HalfEdge LeftBound(Site p) {
            var he = LeftEnd;
            do {
                he = he.Right;
            } while (he != RightEnd && Geometry.RightOf(he, p));
            he = he.Left;
            return he;
        }

        public void Delete(HalfEdge he) {
            he.Left.Right = he.Right;
            he.Right.Left = he.Left;
            he.Edge = null;
        }

        public Site LeftRegion(HalfEdge he) {
            if (he.Edge == null) {
                return _siteList.BottomSite;
            }
            return he.Side == Side.Left ? he.Edge.Region[Side.Left] : he.Edge.Region[Side.Right];
        }
        public Site RightRegion(HalfEdge he) {
            if (he.Edge == null) {
                return _siteList.BottomSite;
            }
            return he.Side == Side.Left ? he.Edge.Region[Side.Right] : he.Edge.Region[Side.Left];
        }
    }
}