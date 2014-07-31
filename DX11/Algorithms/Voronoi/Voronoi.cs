using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Core;

namespace Algorithms.Voronoi {
    public class Voronoi {
        
        private SiteList _sites;
        private readonly Dictionary<Point, Site> _sitesIndexedByLocation;
        private List<Triangle> _triangles;
        public List<Edge> Edges { get; private set; }
        public Rectangle PlotBounds { get; private set; }
        public List<Vertex> Vertices { get; private set; }

        public Voronoi(List<Point> points, List<int> colors, Rectangle plotBounds) {
            _sitesIndexedByLocation = new Dictionary<Point, Site>();
            _triangles = new List<Triangle>();
            Edges = new List<Edge>();
            PlotBounds = plotBounds;

            AddSites(points, colors);
            FortunesAlgorithm();
        }

        public List<Point> Region(Point p) {
            if (!_sitesIndexedByLocation.ContainsKey(p)) {
                return new List<Point>();
            }
            var site = _sitesIndexedByLocation[p];
            return site.Region(PlotBounds);
        }

        public List<Point> NeighborSitesForSite(Point p) {
            var points = new List<Point>();
            if (!_sitesIndexedByLocation.ContainsKey(p)) {
                return new List<Point>();
            }
            var site = _sitesIndexedByLocation[p];
            var sites = site.NeighborSites();
            return sites.Select(s => s.Coord).ToList();
        }
        public List<Circle> Circles { get { return _sites.Circles(); } }

        public List<LineSegment> VoronoiBoundaryForSite(Point coord) {
            var r = SelectEdgesForSitePoint(coord, Edges);
            return VisibleLineSegments(r);
        }

        public List<LineSegment> DelauneyLinesForSite(Point coord) {
            var r = SelectEdgesForSitePoint(coord, Edges);
            return DelauneyLinesForEdges(r);
        }

        public List<LineSegment> VoronoiDiagram() {
            return VisibleLineSegments(Edges);
        }

        public List<LineSegment> DelauneyTriangulation() {
            return DelauneyLinesForEdges(SelectNonIntersectingEdges(Edges));
        } 

        public List<LineSegment> Hull() {
            var r = HullEdges();
            return DelauneyLinesForEdges(r);
        }

        public List<Point> HullPointsInOrder() {
            var hEdges = HullEdges();
            var points = new List<Point>();
            if (hEdges.Count == 0) {
                return points;
            }
            var reorderer = new EdgeReorderer(hEdges, Criterion.cSite);
            hEdges = reorderer.Edges;
            var orientations = reorderer.EdgeOrientations;
            for (int i = 0; i < hEdges.Count; i++) {
                var edge = hEdges[i];
                var orientation = orientations[i];
                points.Add(edge.Sites[orientation].Coord);
            }
            return points;

        }

        public List<LineSegment> SpanningTree(KruskalType type = KruskalType.Minimum) {
            var edges = SelectNonIntersectingEdges(Edges);
            var segments = DelauneyLinesForEdges(edges);
            return Kruskal.KruskalTree(segments, type);
        }
        public List<List<Point>> Regions() {
            return _sites.Regions(PlotBounds);
        }
        public void RegionsPrepare() { _sites.RegionsPrepare(PlotBounds);}
        public List<int> SiteColors() {
            return _sites.SiteColors();
        }

        public Point NearestSitePoint(float x, float y) {
            return _sites.NearestSitePoint(x, y);
        }

        public List<Point> SiteCoords() {
            return _sites.SiteCoords();
        }
 

        public static int CompareByYThenX(ICoord s1, ICoord s2) {
            if (s1.Y < s2.Y) return -1;
            if (s1.Y > s2.Y) return 1;
            if (s1.X < s2.X) return -1;
            if (s1.X > s2.X) return 1;
            return 0;
        }

        public static int CompareByYThenX(ICoord s1, Point s2) {
            if (s1.Y < s2.Y) return -1;
            if (s1.Y > s2.Y) return 1;
            if (s1.X < s2.X) return -1;
            if (s1.X > s2.X) return 1;
            return 0;
        }

        private void AddSites(List<Point> sites, List<int> colors) {
            if (_sites == null) {
                _sites = new SiteList();
            }
            for (int i = 0; i < sites.Count; i++) {
                AddSite(sites[i], colors!=null?colors[i]:0, i);
            }
        }
        private static readonly Random rand = new Random();
        

        private void AddSite(Point p, int color, int index) {
            float weight = rand.Next()*100;
            var site = Site.Create(p, index, weight, color);
            _sites.Push(site);
            _sitesIndexedByLocation[p] = site;
        }

        private List<Edge> HullEdges() {
            return Edges.Where(e => e.IsPartOfConvexHull).ToList();
        }

        private Site LeftRegion(HalfEdge he, Site bottomMostSite) {
            var edge = he.Edge;
            if (edge == null) {
                return bottomMostSite;
            }
            return edge.Sites[he.LeftRight];
        }

        private Site RightRegion(HalfEdge he, Site bottomMostSite) {
            var edge = he.Edge;
            if (edge == null) {
                return bottomMostSite;
            }
            return edge.Sites[he.LeftRight];
        }

        private void FortunesAlgorithm() {
            var dataBounds = _sites.SiteBounds();

            var sqrtNSites = (int)MathF.Sqrt(_sites.Length + 4);
            var heap = new HalfEdgePriorityQueue(dataBounds.Y, dataBounds.Height, sqrtNSites);
            var edgelist = new EdgeList(dataBounds.X, dataBounds.Width, sqrtNSites);

            var halfEdges = new List<HalfEdge>();
            Vertices = new List<Vertex>();
            var bottomMostSite = _sites.Next();
            var newSite = _sites.Next();

            Point newIntStar=null;

            while (true) {
                if (!heap.Empty) {
                    newIntStar = heap.Min();
                }
                if (newSite != null && (heap.Empty || CompareByYThenX(newSite, newIntStar) < 0)) {
                    // new site is smallest
                    Console.WriteLine(newSite);

                    // Step 8:
                    var lbnd = edgelist.EdgeListLeftNeighbor(newSite.Coord); // the Halfedge just to the left of newSite
                    var rbnd = lbnd.EdgeListRightNeighbor; // the Halfedge just to the right
                    var bottomSite = RightRegion(lbnd, bottomMostSite); // this is the same as leftRegion(rbnd)
                    // this Site determines the region containing the new site

                    // Step 9:
                    var edge = Edge.CreateBisectingEdge(bottomSite, newSite);
                    Edges.Add(edge);

                    var bisector = HalfEdge.Create(edge, LR.Side.Left);
                    halfEdges.Add(bisector);
                    // inserting two Halfedges into edgeList constitutes Step 10:
                    // insert bisector to the right of lbnd:
                    edgelist.Insert(lbnd, bisector);

                    // first half of Step 11:
                    var vertex = Vertex.Intersect(lbnd, bisector);
                    if (vertex != null) {
                        Vertices.Add(vertex);
                        heap.Remove(lbnd);
                        lbnd.Vertex = vertex;
                        lbnd.YStar = vertex.Y + newSite.Distance(vertex);
                        heap.Insert(lbnd);
                    }
                    lbnd = bisector;
                    bisector = HalfEdge.Create(edge, LR.Side.Right);
                    halfEdges.Add(bisector);
                    // second Halfedge for Step 10:
                    // insert bisector to the right of lbnd:
                    edgelist.Insert(lbnd, bisector);

                    // second half of Step 11:
                    vertex = Vertex.Intersect(bisector, rbnd);
                    if (vertex != null) {
                        Vertices.Add(vertex);
                        bisector.Vertex = vertex;
                        bisector.YStar = vertex.Y + newSite.Distance(vertex);
                        heap.Insert(bisector);
                    }
                    newSite = _sites.Next();
                } else if (heap.Empty == false) {
                    //intersection is smallest
                    var lbnd = heap.ExtractMin();
                    var llbnd = lbnd.EdgeListLeftNeighbor;
                    var rbnd = lbnd.EdgeListRightNeighbor;
                    var rrbnd = rbnd.EdgeListRightNeighbor;
                    var bottomSite = LeftRegion(lbnd, bottomMostSite);
                    var topSite = RightRegion(rbnd, bottomMostSite);
                    // these three sites define a Delaunay triangle
                    _triangles.Add(new Triangle(bottomSite, topSite, RightRegion(lbnd, bottomMostSite)));

                    var v = lbnd.Vertex;
                    v.SetIndex();
                    lbnd.Edge.SetVertex(lbnd.LeftRight, v);
                    rbnd.Edge.SetVertex(rbnd.LeftRight, v);
                    edgelist.Remove(lbnd);
                    heap.Remove(rbnd);
                    edgelist.Remove(rbnd);
                    var leftRight = LR.Side.Left;
                    if (bottomSite.Y > topSite.Y) {
                        var tempSite = bottomSite;
                        bottomSite = topSite;
                        topSite = tempSite;
                        leftRight = LR.Side.Right;
                    }
                    var edge = Edge.CreateBisectingEdge(bottomSite, topSite);
                    Edges.Add(edge);
                    var bisector = HalfEdge.Create(edge, leftRight);
                    halfEdges.Add(bisector);
                    edgelist.Insert(llbnd, bisector);
                    edge.SetVertex(LR.Other(leftRight), v);
                    var vertex = Vertex.Intersect(llbnd, bisector);
                    if (vertex != null) {
                        Vertices.Add(vertex);
                        heap.Remove(llbnd);
                        llbnd.Vertex = vertex;
                        llbnd.YStar = vertex.Y + bottomSite.Distance(vertex);
                        heap.Insert(llbnd);
                    }
                    vertex = Vertex.Intersect(bisector, rrbnd);
                    if (vertex != null) {
                        Vertices.Add(vertex);
                        bisector.Vertex = vertex;
                        bisector.YStar = vertex.Y + bottomSite.Distance(vertex);
                        heap.Insert(bisector);
                    }
                } else {
                    break;
                }
            }
            foreach (var edge in Edges) {
                edge.ClipVertices(PlotBounds);
            }
        }

        


        public static List<LineSegment> DelauneyLinesForEdges(IEnumerable<Edge> edges) {
            return edges.Select(e => e.DelauneyLine).ToList();
        }

        public static List<LineSegment> VisibleLineSegments(IEnumerable<Edge> edges) {
            return (edges.Where(edge => edge.Visible).Select(edge => new LineSegment(edge.ClippedEnds[LR.Side.Left], edge.ClippedEnds[LR.Side.Right]))).ToList();
        }

        public static List<Edge> SelectEdgesForSitePoint(Point p, IEnumerable<Edge> edges) {
            return edges.Where(edge => (edge.LeftSite != null && edge.LeftSite.Coord == p) || edge.RightSite != null && edge.RightSite.Coord == p).ToList();
        }

        public static List<Edge> SelectNonIntersectingEdges(List<Edge> edges) {
            return edges;
        } 
    }

    public enum KruskalType {
        Minimum = 0,
        Maximum
    }
}