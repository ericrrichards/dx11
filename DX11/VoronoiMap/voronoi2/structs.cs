using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlimDX;

namespace VoronoiMap.voronoi2 {
    public class Site {
        public Vector2 Coord;
        public int SiteID;
    }

    public class Edge {
        public float A, B, C;
        public Dictionary<LR, Site> EP = new Dictionary<LR, Site>();
        public Dictionary<LR, Site> Reg = new Dictionary<LR, Site>();
        public int EdgeID;
        public static readonly Edge Deleted = new Edge();
    }

    public class HalfEdge {
        public HalfEdge EdgeListRight;
        public HalfEdge EdgeListLeft;
        public Edge Edge;
        public LR LeftRight;
        public Site Vertex;
        public float YStar;
        public HalfEdge PriorityQueueNext;

        private HalfEdge(){}

        public static HalfEdge Create(Edge e, LR lr) {
            var answer = new HalfEdge {
                Edge = e,
                LeftRight = lr,
                PriorityQueueNext = null,
                Vertex = null
            };
            return answer;

        }
        public class Voronoi {
            public Voronoi(List<Vector2> points, Rectangle bounds) {
                Geometry.Init(points.Count, bounds);
                Edges = new List<Edge>();
                var sites = new List<Site>();
                for (int i = 0; i < points.Count; i++) {
                    sites.Add(new Site(){Coord = points[i], SiteID = i});
                }
                Compute(sites);
            }


            public List<Edge> Edges { get; private set; } 
            public void Compute(List<Site> sites) {
                var pq = new PriorityQueue(Geometry.SqrtNumSites);
                var el = new EdgeList(Geometry.SqrtNumSites);
                var i = 0;
                el.BottomSite = sites[i++];
                Out.Site(el.BottomSite);
                var newSite = sites[i++];
                var newIntStar = new Vector2(float.MaxValue);
                while (true) {
                    if (!pq.Empty) {
                        newIntStar = pq.Min();
                    }
                    if (newSite != null &&
                        (pq.Empty || newSite.Coord.Y < newIntStar.Y || (newSite.Coord.Y == newIntStar.X && newSite.Coord.X < newIntStar.X))) {
                        Out.Site(newSite);

                        var lbnd = el.LeftBound(newSite.Coord);
                        var rbnd = el.Right(lbnd);
                        var bot = el.RightRegion(lbnd);
                        var e = Geometry.Bisect(bot, newSite);
                        var bisector = HalfEdge.Create(e, LR.Left);
                        el.Insert(lbnd, bisector);
                        var p = Geometry.Intersect(lbnd, bisector);
                        if (p != null) {
                            pq.Delete(lbnd);
                            pq.Insert(lbnd, p, Geometry.Dist(p, newSite));
                        }
                        lbnd = bisector;
                        bisector = HalfEdge.Create(e, LR.Right);
                        el.Insert(lbnd, bisector);
                        p = Geometry.Intersect(bisector, rbnd);
                        if (p != null) {
                            pq.Insert(bisector, p, Geometry.Dist(p, newSite));
                        }
                        newSite = sites[i++];
                    } else if (!pq.Empty) {
                        var lbnd = pq.ExtractMin();
                        var llbnd = el.Left(lbnd);
                        var rbnd = el.Right(lbnd);
                        var rrbnd = el.Right(rbnd);
                        var bot = el.LeftRegion(lbnd);
                        var top = el.RightRegion(rbnd);
                        Out.Triplet(bot, top, el.RightRegion(lbnd));
                        var v = lbnd.Vertex;
                        Geometry.Makevertex(v);
                        Geometry.Endpoint(lbnd.Edge, lbnd.LeftRight, v);
                        Geometry.Endpoint(rbnd.Edge, rbnd.LeftRight, v);
                        el.Delete(lbnd);
                        pq.Delete(rbnd);
                        el.Delete(rbnd);
                        var lr = LR.Left;
                        if (bot.Coord.X > top.Coord.Y) {
                            var temp = bot;
                            bot = top;
                            top = temp;
                            lr = LR.Right;
                        }
                        var e = Geometry.Bisect(bot, top);
                        var bisector = HalfEdge.Create(e, lr);
                        el.Insert(llbnd, bisector);
                        Geometry.Endpoint(e, LR.Other(lr), v);
                        var p = Geometry.Intersect(llbnd, bisector);
                        if (p != null) {
                            pq.Delete(llbnd);
                            pq.Insert(llbnd, p, Geometry.Dist(p, bot));
                        }
                        p = Geometry.Intersect(bisector, rrbnd);
                        if (p != null) {
                            pq.Insert(bisector, p, Geometry.Dist(p, bot));
                        }
                    } else {
                        break;
                    }
                }
                for (var lbnd = el.Right(el.LeftEnd); lbnd != el.RightEnd; lbnd = el.Right(lbnd)) {
                    var e = lbnd.Edge;
                    Out.Endpoint(e);
                    Edges.Add(e);
                }
            }
        }
    }


    public class LR {
        public static LR Left;
        public static LR Right;
        public static LR Other(LR lr) {
            return lr == Left ? Right : Left;
        }
        private LR(){}
    }

}
