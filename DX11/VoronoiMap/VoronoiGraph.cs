using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using log4net;

namespace VoronoiMap {
    public class VoronoiGraph {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public bool Debug { get; set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public readonly List<Site> Sites = new List<Site>();
        public readonly List<Site> Vertices = new List<Site>();
        public readonly List<Segment> Segments = new List<Segment>();
        public readonly List<Triangle> Triangles = new List<Triangle>();
        public readonly List<Edge> Edges = new List<Edge>();
        public float SweepLine { get; set; }

        public VoronoiGraph(int width = 800, int height = 600) {
            Width = width;
            Height = height;
            Debug = false;
        }


        public static VoronoiGraph ComputeVoronoi(IEnumerable<PointF> points, int w = 800, int h = 600, bool debug=false) {
            var sites = new SiteList(points);
            sites.LogSites();
            var graph = new VoronoiGraph(w, h) { Debug = debug };
            try {
                var edgeList = new EdgeList(sites);
                var eventQueue = new EventQueue();

                sites.BottomSite = sites.ExtractMin();

                graph.PlotSite(sites.BottomSite);

                var newSite = sites.ExtractMin();
                var newIntStar = new Site(Single.MaxValue, Single.MaxValue);

                while (true) {
                    if (!eventQueue.IsEmpty) {
                        newIntStar = eventQueue.Min();
                    }

                    if (newSite != null && (eventQueue.IsEmpty || newSite.CompareTo(newIntStar) < 0)) {
                        // new site is smallest
                        graph.PlotSite(newSite);

                        var lbnd = edgeList.LeftBound(newSite);
                        var rbnd = lbnd.Right;

                        var bot = edgeList.RightRegion(lbnd);

                        var e = Geometry.Bisect(bot, newSite);
                        graph.PlotBisector(e);

                        var bisector = new HalfEdge(e, Side.Left);
                        EdgeList.Insert(lbnd, bisector);

                        var p = Geometry.Intersect(lbnd, bisector);
                        if (p != null) {
                            eventQueue.Delete(lbnd);
                            if (debug) {
                                Console.WriteLine("Inserting {0}", p);
                            }
                            eventQueue.Insert(lbnd, p, Geometry.Distance(p, newSite));
                        }

                        lbnd = bisector;
                        bisector = new HalfEdge(e, Side.Right);
                        EdgeList.Insert(lbnd, bisector);

                        p = Geometry.Intersect(bisector, rbnd);
                        if (p != null) {
                            if (debug) {
                                Console.WriteLine("Inserting {0}", p);
                            }
                            eventQueue.Insert(bisector, p, Geometry.Distance(p, newSite));
                        }
                        newSite = sites.ExtractMin();

                    } else if (!eventQueue.IsEmpty) {
                        // intersection is smallest
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
                        EdgeList.Delete(lbnd);
                        eventQueue.Delete(rbnd);
                        EdgeList.Delete(rbnd);

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
                        EdgeList.Insert(llbnd, bisector);
                        Geometry.EndPoint(e, Side.Other(pm), v, graph);
                        var p = Geometry.Intersect(llbnd, bisector);
                        if (p != null) {
                            eventQueue.Delete(llbnd);
                            if (debug) {
                                Console.WriteLine("Inserting {0}", p);
                            }
                            eventQueue.Insert(llbnd, p, Geometry.Distance(p, bot));
                        }
                        p = Geometry.Intersect(bisector, rrbnd);
                        if (p != null) {
                            if (debug) {
                                Console.WriteLine("Inserting {0}", p);
                            }
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
            graph.SweepLine = graph.Height;
            graph.ResetNewItems();
            foreach (var edge in graph.Edges) {
                edge.ClipVertices(new Rectangle(0,0, w, h));
            }
            
            return graph;
        }

        public void PlotSite(Site site) {
            site.New = true;
            Sites.Add(site);
            if (Debug) {
                Console.WriteLine("site {0}", site);
                Log.InfoFormat("site {0}", site);
            }
            if (site.Y > SweepLine) {
                SweepLine = site.Y;
            }

        }


        public void PlotBisector(Edge e) {
            if (Debug) {
                Console.WriteLine("bisector {0} {1}", e.Region[Side.Left], e.Region[Side.Right]);
                Log.InfoFormat("bisector {0} {1}", e.Region[Side.Left], e.Region[Side.Right]);
            }
            Edges.Add(e);
        }

        public void PlotEndpoint(Edge e) {
            if (Debug) {
                Console.WriteLine("EP {0}", e);
                Log.InfoFormat("EP {0}", e);
            }
            ClipLine(e);
        }

        public void PlotVertex(Site s) {
            if (Debug) {
                Console.WriteLine("vertex {0},{1}", s.X, s.Y);
                Log.InfoFormat("vertex {0},{1}", s.X, s.Y);
            }
            s.New = true;
            Vertices.Add(s);
        }

        public void PlotTriple(Site s1, Site s2, Site s3) {
            if (Debug) {
                Console.WriteLine("triple {0} {1} {2}", s1, s2, s3);
            }
            var triangle = new Triangle(s1, s2, s3) { New = true };
            Triangles.Add(triangle);
        }

        public void ResetNewItems() {
            foreach (var site in Sites) {
                site.New = false;
            }
            foreach (var vertex in Vertices) {
                vertex.New = false;
            }
            foreach (var segment in Segments) {
                segment.New = false;
            }
            foreach (var triangle in Triangles) {
                triangle.New = false;
            }
        }


        /// <summary>
        /// Somewhat redundant line clipping routine
        /// </summary>
        /// <param name="e"></param>
        private void ClipLine(Edge e) {
            var clipped = e.GetClippedEnds(new Rectangle(0, 0, Width, Height));
            if (clipped != null) {
                var site1 = new Site(clipped.Item1);
                var site2 = new Site(clipped.Item2);
                var s = new Segment(site1, site2) {
                    New = true
                };
                Segments.Add(s);
            }
        }
    }
}