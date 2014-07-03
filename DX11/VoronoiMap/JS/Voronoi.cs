using System;
using System.Collections.Generic;
using System.Drawing;

namespace Fortune.FromJS {
    public static class Voronoi {
        public static VoronoiGraph ComputeVoronoi(IEnumerable<Point> points, int w=800, int h=600) {
            var sites = new SiteList(points);
            var graph = new VoronoiGraph(w,h){Debug = true};
            try {
                var edgeList = new EdgeList(sites);
                var eventQueue = new EventQueue();
                    
                sites.BottomSite = sites.ExtractMin();

                graph.PlotSite(sites.BottomSite);

                var newSite = sites.ExtractMin();
                var newIntStar = new Site(float.MaxValue, float.MaxValue);

                while (true) {
                    if (!eventQueue.IsEmpty) {
                        newIntStar = eventQueue.Min();
                    }
                    // new site is smallest
                    if (newSite != null && (eventQueue.IsEmpty ||
                                            newSite.Y < newIntStar.Y ||
                                            (Math.Abs(newSite.Y - newIntStar.Y) < Geometry.Tolerance && newSite.X < newIntStar.X))) {

                        graph.PlotSite(newSite);

                        var lbnd = edgeList.LeftBound(newSite);
                        var rbnd = lbnd.Right;
                        var bot = edgeList.RightRegion(lbnd);
                        var e = Geometry.Bisect(bot, newSite);
                        graph.PlotBisector(e);
                        var bisector = new HalfEdge(e, Side.Left);
                        edgeList.Insert(lbnd, bisector);
                        var p = Geometry.Intersect(lbnd, bisector);
                        if (p != null) {
                            eventQueue.Delete(lbnd);
                            eventQueue.Insert(lbnd, p, Geometry.Distance(p, newSite));
                        }
                        lbnd = bisector;
                        bisector = new HalfEdge(e, Side.Right);
                        edgeList.Insert(lbnd, bisector);
                        p = Geometry.Intersect(bisector, rbnd);
                        if (p != null) {
                            eventQueue.Insert(bisector, p, Geometry.Distance(p, newSite));
                        }
                        newSite = sites.ExtractMin();
                    } else if (!eventQueue.IsEmpty) { // intersection is smallest
                        var lbnd = eventQueue.ExtractMin();
                        var llbnd = lbnd.Left;
                        var rbnd = lbnd.Right;
                        var rrbnd = rbnd.Right;
                        var bot = edgeList.LeftRegion(lbnd);
                        var top = edgeList.RightRegion(rbnd);
                        graph.PlotTriple(bot, top, edgeList.RightRegion(lbnd));
                        var v = lbnd.Vertex;

                        graph.PlotVertex(v);

                        Geometry.EndPoint(lbnd.Edge, lbnd.Side, v, graph);
                        Geometry.EndPoint(rbnd.Edge, rbnd.Side, v, graph);
                        edgeList.Delete(lbnd);
                        eventQueue.Delete(rbnd);
                        edgeList.Delete(rbnd);
                        var pm = Side.Left;
                        if (bot.Y > top.Y) {
                            var temp = bot;
                            bot = top;
                            top = temp;
                            pm = Side.Right;
                        }
                        var e = Geometry.Bisect(bot, top);
                        graph.PlotBisector(e);
                        var bisector = new HalfEdge(e, pm);
                        edgeList.Insert(llbnd, bisector);
                        Geometry.EndPoint(e, pm == Side.Left ? Side.Right : Side.Left, v, graph);
                        var p = Geometry.Intersect(llbnd, bisector);
                        if (p != null) {
                            eventQueue.Delete(llbnd);
                            eventQueue.Insert(llbnd, p, Geometry.Distance(p, bot));
                        }
                        p = Geometry.Intersect(bisector, rrbnd);
                        if (p != null) {
                            eventQueue.Insert(bisector, p, Geometry.Distance(p, bot));
                        }
                    } else {
                        break;
                    }
                }
                for (var lbnd = edgeList.LeftEnd.Right; lbnd != edgeList.RightEnd; lbnd = lbnd.Right) {
                    var e = lbnd.Edge;
                    graph.PlotEndpoint(e);
                }
            } catch (Exception ex) {
                Console.WriteLine("########################################");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            return graph;
        }

    }
}