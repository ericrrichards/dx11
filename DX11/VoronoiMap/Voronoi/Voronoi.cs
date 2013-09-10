using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using Core;
using SlimDX;

namespace VoronoiMap.Voronoi {
    class Voronoi {
        private SiteList _sites;
        private Dictionary<Vector2, Site> _sitesIndexedByLocation;
        private List<Triangle> _triangles;
        public List<Edge> Edges { get; private set; }
        public Rectangle PlotBounds { get; private set; }


        public Voronoi(List<Vector2> points, List<Color4> colors, Rectangle plotBounds) {
            _sites = new SiteList();
            _sitesIndexedByLocation = new Dictionary<Vector2, Site>();
            AddSites(points, colors);
            PlotBounds = plotBounds;
            _triangles = new List<Triangle>();
            Edges = new List<Edge>();
            FortunesAlgorithm();
        }



        private void AddSites(List<Vector2> points, List<Color4> colors) {
            for (int i = 0; i < points.Count; i++) {
                AddSite(points[i], colors != null ? colors[i] : Color.Black, i);
            }
        }

        private void AddSite(Vector2 p, Color4 color, int index) {
            if (_sitesIndexedByLocation.ContainsKey(p)) return;
            
            var weight = MathF.Rand(0, 1.0f) * 100;
            var site = new Site(p, index, weight, color);
            _sites.Push(site);
            _sitesIndexedByLocation[p] = site;
        }

        private void FortunesAlgorithm() {
            var dataBounds = _sites.GetSiteBounds();
            var sqrtNSites = (int)Math.Sqrt(_sites.Length + 4);
            var heap = new HalfEdgePriorityQueue(dataBounds.Top, dataBounds.Height, sqrtNSites);
            var edgeList = new EdgeList(dataBounds.Left, dataBounds.Width, sqrtNSites);
            var halfEdges = new List<HalfEdge>();
            var vertices = new List<Vertex>();

            var bottomMostSite = _sites.Next();
            var newSite = _sites.Next();

            var newIntStar = new Vector2();

            for (; ; ) {
                if (!heap.Empty) {
                    newIntStar = heap.Min();
                }
                if (newSite != null && (heap.Empty || CompareByYThenX(newSite, newIntStar) < 0)) {
                    Console.WriteLine("smallest: new site " + newSite);
                    var lbnd = edgeList.EdgeListLeftNeighbor(newSite.Coord);
                    Console.WriteLine("lbnd: " + lbnd);
                    var rbnd = lbnd.EdgeListRightNeighbor;
                    Console.WriteLine("rbnd: " + rbnd);
                    var bottomSite = RightRegion(lbnd) ?? bottomMostSite;
                    Console.WriteLine("new site is in region of existing site: " + bottomSite);

                    var edge = Edge.CreateBisectingEdge(bottomSite, newSite);
                    Console.WriteLine("new edge: " + edge);
                    Edges.Add(edge);

                    var bisector = new HalfEdge(edge, LR.Left);
                    halfEdges.Add(bisector);

                    edgeList.Insert(lbnd, bisector);

                    Vertex vertex;
                    if ((vertex = Vertex.Intersect(lbnd, bisector)) != null) {
                        vertices.Add(vertex);
                        heap.Remove(lbnd);
                        lbnd.Vertex = vertex;
                        lbnd.YStar = vertex.Y + newSite.Distance(vertex);
                        heap.Insert(lbnd);
                    }
                    lbnd = bisector;
                    bisector = new HalfEdge(edge, LR.Right);
                    halfEdges.Add(bisector);

                    edgeList.Insert(lbnd, bisector);

                    if ((vertex = Vertex.Intersect(bisector, rbnd)) != null) {
                        vertices.Add(vertex);
                        bisector.Vertex = vertex;
                        bisector.YStar = vertex.Y + newSite.Distance(vertex);
                        heap.Insert(bisector);
                    }
                    newSite = _sites.Next();
                } else if (!heap.Empty) {
                    var lbnd = heap.ExtractMin();
                    var llbnd = lbnd.EdgeListLeftNeighbor;
                    var rbnd = lbnd.EdgeListRightNeighbor;
                    var rrbnd = rbnd.EdgeListRightNeighbor;
                    var bottomSite = LeftRegion(lbnd) ?? bottomMostSite;
                    var topSite = RightRegion(rbnd) ?? bottomMostSite;

                    var v = lbnd.Vertex;
                    if (v == null) {
                        Debugger.Break();
                    }
                    v.SetIndex();
                    lbnd.Edge.SetVertex(lbnd.LeftRight, v);
                    rbnd.Edge.SetVertex(rbnd.LeftRight, v);
                    edgeList.Remove(lbnd);
                    heap.Remove(rbnd);
                    edgeList.Remove(rbnd);
                    var leftRight = LR.Left;
                    if (bottomSite.Y > topSite.Y) {
                        var tempSite = bottomSite;
                        bottomSite = topSite;
                        topSite = tempSite;
                        leftRight = LR.Right;
                    }
                    var edge = Edge.CreateBisectingEdge(bottomSite, topSite);
                    Edges.Add(edge);
                    var bisector = new HalfEdge(edge, leftRight);
                    halfEdges.Add(bisector);
                    edgeList.Insert(llbnd, bisector);
                    if (v == null) {
                        Debugger.Break();
                    }
                    edge.SetVertex(LR.Other(leftRight), v);
                    
                    Vertex vertex;
                    if ((vertex = Vertex.Intersect(llbnd, bisector)) != null) {
                        vertices.Add(vertex);
                        heap.Remove(llbnd);
                        llbnd.Vertex = vertex;
                        llbnd.YStar = vertex.Y + bottomSite.Distance(vertex);
                        heap.Insert(llbnd);
                    }
                    if ((vertex = Vertex.Intersect(bisector, rrbnd)) != null) {
                        vertices.Add(vertex);
                        bisector.Vertex = vertex;
                        bisector.YStar = vertex.Y + bottomSite.Distance(vertex);
                        heap.Insert(bisector);
                    }
                } else {
                    break;
                }
            }
            halfEdges.Clear();


            var nullVerts = Edges.Where(e => e.RightVertex == null || e.LeftVertex == null).ToList();
            foreach (var edge in Edges) {
                edge.ClipVertices(PlotBounds);
            }
            vertices.Clear();
        }

        private static Site LeftRegion(HalfEdge he) {
            var edge = he.Edge;
            return edge == null ? null : edge.Site(he.LeftRight);
        }

        private static Site RightRegion(HalfEdge he) {
            var edge = he.Edge;
            return edge == null ? null : edge.Site(LR.Other(he.LeftRight));
        }

        public static int CompareByYThenX(Site s1, ICoord s2) {
            if (s1.Y < s2.Y) return -1;
            if (s1.Y > s2.Y) return 1;
            if (s1.X < s2.X) return -1;
            if (s1.X > s2.X) return 1;

            return 0;
        }
        public static int CompareByYThenX(Site s1, Vector2 s2) {
            if (s1.Y < s2.Y) return -1;
            if (s1.Y > s2.Y) return 1;
            if (s1.X < s2.X) return -1;
            if (s1.X > s2.X) return 1;

            return 0;
        }

        public List<Vector2> Region(Vector2 p) {
            var site = _sitesIndexedByLocation[p];
            if (site == null) {
                return new List<Vector2>();
            }
            return site.Region(PlotBounds);
        }
    }

    public class Triangle {

    }
}