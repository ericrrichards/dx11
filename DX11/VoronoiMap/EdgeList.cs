namespace VoronoiMap {
    public class EdgeList {
        public HalfEdge LeftEnd { get; private set; }
        public HalfEdge RightEnd { get; private set; }

        private readonly SiteList _siteList;

        public EdgeList( SiteList siteList) {
            _siteList = siteList;
            LeftEnd = new HalfEdge(null, Side.Left);
            RightEnd = new HalfEdge(null, Side.Left);
            LeftEnd.Right = RightEnd;
            RightEnd.Left = LeftEnd;
        }

        public static void Insert(HalfEdge lb, HalfEdge he) {
            he.Left = lb;
            he.Right = lb.Right;
            lb.Right.Left = he;
            lb.Right = he;
        }

        public static void Delete(HalfEdge he) {
            he.Left.Right = he.Right;
            he.Right.Left = he.Left;
            he.Edge = null;
        }

        public HalfEdge LeftBound(Site p) {
            var he = LeftEnd;
            do {
                he = he.Right;
            } while (he != RightEnd && he.RightOf(p));
            he = he.Left;
            return he;
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