using System;
using System.Collections.Generic;
using System.Drawing;

namespace VoronoiMap {

    /// <summary>
    /// Adapted from http://philogb.github.io/blog/2010/02/12/voronoi-tessellation/
    /// Also uses portions from https://github.com/SirAnthony/cppdelaunay
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


        public Voronoi(IEnumerable<PointF> points, int w = 800, int h = 600, bool debug=false) {
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

                if (_newSite != null && (_eventQueue.IsEmpty || /*Geometry.CompareByYThenX(_newSite, _newIntStar)*/ _newSite.CompareTo(_newIntStar) < 0 )) {

                    _graph.PlotSite(_newSite);

                    var lbnd = _edgeList.LeftBound(_newSite);
                    var rbnd = lbnd.Right;
                    var bot = _edgeList.RightRegion(lbnd);
                    var e = Geometry.Bisect(bot, _newSite);
                    _graph.PlotBisector(e);
                    var bisector = new HalfEdge(e, Side.Left);
                    EdgeList.Insert(lbnd, bisector);
                    var p = Geometry.Intersect(lbnd, bisector);
                    if (p != null) {
                        _eventQueue.Delete(lbnd);
                        if (_graph.Debug) {
                            Console.WriteLine("Inserting {0}", p);
                        }
                        _eventQueue.Insert(lbnd, p, Geometry.Distance(p, _newSite));
                    }
                    lbnd = bisector;
                    bisector = new HalfEdge(e, Side.Right);
                    EdgeList.Insert(lbnd, bisector);
                    p = Geometry.Intersect(bisector, rbnd);
                    if (p != null) {
                        if (_graph.Debug) {
                            Console.WriteLine("Inserting {0}", p);
                        }
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
                    EdgeList.Delete(lbnd);
                    _eventQueue.Delete(rbnd);
                    EdgeList.Delete(rbnd);
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
                    EdgeList.Insert(llbnd, bisector);
                    Geometry.EndPoint(e, pm == Side.Left ? Side.Right : Side.Left, v, _graph);
                    var p = Geometry.Intersect(llbnd, bisector);
                    if (p != null) {
                        _eventQueue.Delete(llbnd);
                        if (_graph.Debug) {
                            Console.WriteLine("Inserting {0}", p);
                        }
                        _eventQueue.Insert(llbnd, p, Geometry.Distance(p, bot));
                    }
                    p = Geometry.Intersect(bisector, rrbnd);
                    if (p != null) {
                        if (_graph.Debug) {
                            Console.WriteLine("Inserting {0}", p);
                        }
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
                    EdgeList.Delete(lbnd);
                    StepNumber++;
                } else {
                    foreach (var edge in _graph.Edges) {
                        edge.ClipVertices(new Rectangle(0, 0, _graph.Width, _graph.Height));
                    }
                    if (_graph.Debug) {
                        Console.WriteLine("Done computing graph!");
                    }
                }
            }
            if (_graph.Debug) {
                Console.WriteLine("Step: " + StepNumber);
            }
            return _graph;
        }
    }
}