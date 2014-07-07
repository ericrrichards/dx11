using System;
using System.Collections.Generic;
using System.Drawing;

namespace VoronoiMap {

    /// <summary>
    /// Adapted from http://philogb.github.io/blog/2010/02/12/voronoi-tessellation/
    /// </summary>
    public  class Voronoi {
        private readonly VoronoiGraph _graph;
        private readonly SiteList _sites;
        private readonly EdgeList _edgeList;
        private readonly EventQueue _eventQueue;

        private Site _newSite;
        private Site _newIntStar;

        private bool _edgeFixup;
        public int StepNumber { get; private set; }

        public Voronoi(IEnumerable<Point> points, int w = 800, int h = 600, bool debug=false) {
            _sites = new SiteList(points);
            _sites.LogSites();
            _graph = new VoronoiGraph(w, h) {Debug = debug};
            
            _edgeList = new EdgeList(_sites);
            _eventQueue = new EventQueue();
            _edgeFixup = false;

            StepNumber = 0;

        }

        public VoronoiGraph Initialize() {
            _sites.BottomSite = _sites.ExtractMin();
            _graph.PlotSite(_sites.BottomSite);

            _newSite = _sites.ExtractMin();
            if (_newSite.Y > _graph.SweepLine) {
                _graph.SweepLine = _newSite.Y;
            }


            _newIntStar = new Site(float.MaxValue, float.MaxValue);
            
            return _graph;
        }

        public VoronoiGraph StepVoronoi() {
            _graph.ResetNewItems();
            if (!_edgeFixup) {
                if (!_eventQueue.IsEmpty) {
                    _newIntStar = _eventQueue.Min();
                    
                }

                if (_newSite != null && (_eventQueue.IsEmpty || _newSite.Y < _newIntStar.Y || (Math.Abs(_newSite.Y - _newIntStar.Y) < Geometry.Tolerance && _newSite.X < _newIntStar.X))) {

                    _graph.PlotSite(_newSite);

                    var lbnd = _edgeList.LeftBound(_newSite);
                    var rbnd = lbnd.Right;
                    var bot = _edgeList.RightRegion(lbnd);
                    var e = Geometry.Bisect(bot, _newSite);
                    _graph.PlotBisector(e);
                    var bisector = new HalfEdge(e, Side.Left);
                    _edgeList.Insert(lbnd, bisector);
                    var p = Geometry.Intersect(lbnd, bisector);
                    if (p != null) {
                        _eventQueue.Delete(lbnd);
                        Console.WriteLine("Inserting {0}", p);
                        _eventQueue.Insert(lbnd, p, Geometry.Distance(p, _newSite));
                    }
                    lbnd = bisector;
                    bisector = new HalfEdge(e, Side.Right);
                    _edgeList.Insert(lbnd, bisector);
                    p = Geometry.Intersect(bisector, rbnd);
                    if (p != null) {
                        Console.WriteLine("Inserting {0}", p);
                        _eventQueue.Insert(bisector, p, Geometry.Distance(p, _newSite));
                    }
                    _newSite = _sites.ExtractMin();
                    if (_newSite !=null && _newSite.Y > _graph.SweepLine) {
                        _graph.SweepLine = _newSite.Y;
                    } else if (_newSite == null) {
                        _graph.SweepLine = _graph.Height;
                    }

                } else if (!_eventQueue.IsEmpty) { // intersection is smallest
                    var lbnd = _eventQueue.ExtractMin();
                    var llbnd = lbnd.Left;
                    var rbnd = lbnd.Right;
                    var rrbnd = rbnd.Right;
                    var bot = _edgeList.LeftRegion(lbnd);
                    var top = _edgeList.RightRegion(rbnd);
                    _graph.PlotTriple(bot, top, _edgeList.RightRegion(lbnd));
                    var v = lbnd.Vertex;

                    _graph.PlotVertex(v);

                    Geometry.EndPoint(lbnd.Edge, lbnd.Side, v, _graph);
                    Geometry.EndPoint(rbnd.Edge, rbnd.Side, v, _graph);
                    _edgeList.Delete(lbnd);
                    _eventQueue.Delete(rbnd);
                    _edgeList.Delete(rbnd);
                    var pm = Side.Left;
                    if (bot.Y > top.Y) {
                        var temp = bot;
                        bot = top;
                        top = temp;
                        pm = Side.Right;
                    }
                    var e = Geometry.Bisect(bot, top);
                    _graph.PlotBisector(e);
                    var bisector = new HalfEdge(e, pm);
                    _edgeList.Insert(llbnd, bisector);
                    Geometry.EndPoint(e, pm == Side.Left ? Side.Right : Side.Left, v, _graph);
                    var p = Geometry.Intersect(llbnd, bisector);
                    if (p != null) {
                        _eventQueue.Delete(llbnd);
                        Console.WriteLine("Inserting {0}", p);
                        _eventQueue.Insert(llbnd, p, Geometry.Distance(p, bot));
                    }
                    p = Geometry.Intersect(bisector, rrbnd);
                    if (p != null) {
                        Console.WriteLine("Inserting {0}", p);
                        _eventQueue.Insert(bisector, p, Geometry.Distance(p, bot));
                    }
                } else {
                    _edgeFixup = true;
                }
                StepNumber++;
            } else {
                var lbnd = _edgeList.LeftEnd.Right;
                if (lbnd != _edgeList.RightEnd) {
                    var e = lbnd.Edge;
                    _graph.PlotEndpoint(e);
                    _edgeList.Delete(lbnd);
                    StepNumber++;
                } else {
                    Console.WriteLine("Done computing graph!");
                }
            }
            Console.WriteLine("Step: " + StepNumber);
            return _graph;
        }



        public static VoronoiGraph ComputeVoronoi(IEnumerable<Point> points, int w = 800, int h = 600, bool debug=false) {
            var sites = new SiteList(points);
            sites.LogSites();
            var graph = new VoronoiGraph(w, h) { Debug = debug };
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
                    if (newSite != null && 
                        (eventQueue.IsEmpty || newSite.Y < newIntStar.Y || (Math.Abs(newSite.Y - newIntStar.Y) < Geometry.Tolerance && newSite.X < newIntStar.X))) {

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
                            if (debug) {
                                Console.WriteLine("Inserting {0}", p);
                            }
                            eventQueue.Insert(lbnd, p, Geometry.Distance(p, newSite));
                        }
                        lbnd = bisector;
                        bisector = new HalfEdge(e, Side.Right);
                        edgeList.Insert(lbnd, bisector);
                        p = Geometry.Intersect(bisector, rbnd);
                        if (p != null) {
                            if (debug) {
                                Console.WriteLine("Inserting {0}", p);
                            }
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
            return graph;
        }

        
    }
}