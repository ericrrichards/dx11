using System;
using System.Collections.Generic;
using System.Linq;
using SlimDX;

namespace VoronoiMap.Voronoi {
    public class Site : ICoord {

        private const float Epsilon = 0.005f;
        private Color4 _color;
        private float _weight;
        private int _siteIndex;
        public List<Edge> Edges { get; private set; }
        private List<LR> _edgeOrientations;
        private List<Vector2> _region; 

        public Site(Vector2 p, int index, float weight, Color4 color) {
            Init(p, index, weight, color);
        }

        private void Init(Vector2 p, int index, float weight, Color4 color) {
            Coord = p;
            _siteIndex = index;
            _weight = weight;
            _color = color;
            Edges = new List<Edge>();
            _region = null;
        }

        

        public class SiteComparer : IComparer<Site> {
            public int Compare(Site s1, Site s2) {
                var ret = Voronoi.CompareByYThenX(s1, s2);
                if (ret == -1) {
                    if (s1._siteIndex > s2._siteIndex) {
                        var tempIndex = s1._siteIndex;
                        s1._siteIndex = s2._siteIndex;
                        s2._siteIndex = tempIndex;
                    } else if (ret == 1) {
                        var tempIndex = s2._siteIndex;
                        s2._siteIndex = s1._siteIndex;
                        s1._siteIndex = tempIndex;
                    }
                }
                return ret;
            }
        }

        public void AddEdge(Edge edge) {
            Edges.Add(edge);
        }

        public float Distance(ICoord p) {
            return Vector2.Distance(p.Coord, Coord);
        }

        public List<Vector2> Region(Rectangle clipBounds) {
            if (Edges == null || Edges.Count == 0) {
                return new List<Vector2>();
            }
            if (_edgeOrientations == null) {
                ReorderEdges();
                _region = ClipToBounds(clipBounds);
                if ((new Polygon(_region)).Winding == Winding.Clockwise) {
                    _region.Reverse();
                }
            }
            return _region;
        }

        private List<Vector2> ClipToBounds(Rectangle bounds) {
            var points = new List<Vector2>();
            var n = Edges.Count;
            var i = 0;
            while (i < n && (!(Edges[i]).Visible)) {
                i++;
            }
            if (i == n) {
                return new List<Vector2>();
            }
            var edge = Edges[i];
            var orientation = _edgeOrientations[i];
            points.Add(edge.ClippedEnds[orientation]);
            points.Add(edge.ClippedEnds[LR.Other(orientation)]);

            for (int j = i; j < n; j++) {
                edge = Edges[j];
                if (edge.Visible) {
                    Connect(points, j, bounds);
                }
            }
            Connect(points, i, bounds, true);
            return points;
        }

        private void Connect(List<Vector2> points, int j, Rectangle bounds, bool closingUp=false) {
            var rightPoint = points.Last();
            var newEdge = Edges[j];
            var newOrientation = _edgeOrientations[j];
            var newPoint = newEdge.ClippedEnds[newOrientation];
            if (!CloseEnough(rightPoint, newPoint)) {
                if (!Equals(rightPoint.X, newPoint.X) && !Equals(rightPoint.Y, newPoint.Y)) {
                    var rightCheck = BoundsChecker.Check(rightPoint, bounds);
                    var newCheck = BoundsChecker.Check(newPoint, bounds);
                    float px, py;
                    if (rightCheck.HasFlag(BoundsCheck.Right)) {
                        px = bounds.Right;
                        if ( newCheck.HasFlag( BoundsCheck.Bottom)) {
                            py = bounds.Bottom;
                            points.Add(new Vector2(px, py));
                        } else if (newCheck.HasFlag(BoundsCheck.Top)) {
                            py = bounds.Top;
                            points.Add(new Vector2(px, py));
                        } else if (newCheck.HasFlag(BoundsCheck.Left)) {
                            py = rightPoint.Y - bounds.Top + newPoint.Y - bounds.Top < bounds.Height ? bounds.Top : bounds.Bottom;
                            points.Add(new Vector2(px, py));
                            points.Add(new Vector2(bounds.Left, py));
                        }
                    } else if (rightCheck.HasFlag(BoundsCheck.Left)) {
                        px = bounds.Left;
                        if (newCheck.HasFlag(BoundsCheck.Bottom)) {
                            py = bounds.Bottom;
                            points.Add(new Vector2(px,py));
                        } else if (newCheck.HasFlag(BoundsCheck.Top)) {
                            py = bounds.Top;
                            points.Add(new Vector2(px,py));
                        } else if (newCheck.HasFlag(BoundsCheck.Right)) {
                            py = rightPoint.Y - bounds.Top + newPoint.Y - bounds.Top < bounds.Height ? bounds.Top : bounds.Bottom;
                            points.Add(new Vector2(px,py));
                            points.Add(new Vector2(bounds.Right, py));
                        }
                    } else if (rightCheck.HasFlag(BoundsCheck.Top)) {
                        py = bounds.Top;
                        if (newCheck.HasFlag(BoundsCheck.Right)) {
                            px = bounds.Right;
                            points.Add(new Vector2(px,py));
                        } else if (newCheck.HasFlag(BoundsCheck.Left)) {
                            px = bounds.Left;
                            points.Add(new Vector2(px,py));
                        } else if ( newCheck.HasFlag(BoundsCheck.Bottom)){
                            px = rightPoint.X - bounds.Left + newPoint.X - bounds.Left < bounds.Width ? bounds.Left : bounds.Right;
                            points.Add(new Vector2(px,py));
                            points.Add(new Vector2(px, bounds.Bottom));
                        }
                    } else if (rightCheck.HasFlag(BoundsCheck.Bottom)) {
                        py = bounds.Bottom;
                        if (newCheck.HasFlag(BoundsCheck.Right)) {
                            px = bounds.Right;
                            points.Add(new Vector2(px,py));
                        } else if (newCheck.HasFlag(BoundsCheck.Left)) {
                            px = bounds.Left;
                            points.Add(new Vector2(px,py));
                        } else if (newCheck.HasFlag(BoundsCheck.Top)) {
                            px = rightPoint.X - bounds.Left + newPoint.X - bounds.Left < bounds.Width ? bounds.Left : bounds.Right;
                            points.Add(new Vector2(px,py));
                            points.Add(new Vector2(px, bounds.Top));
                        }
                    }
                }
                if (closingUp) {
                    return;
                }
                points.Add(newPoint);
            }
            var newRightPoint = newEdge.ClippedEnds[LR.Other(newOrientation)];
            if (!CloseEnough(points[0], newRightPoint)) {
                points.Add(newRightPoint);
            }
        }

        private bool CloseEnough(Vector2 v1, Vector2 v2) {
            return Vector2.Distance(v1, v2) < Epsilon;
        }

        private void ReorderEdges() {
            var reorderer = new EdgeReorderer(Edges, typeof(Vertex));
            Edges = reorderer.Edges;
            _edgeOrientations = reorderer.EdgeOrientations;

        }
    }

    public class Polygon {
        private readonly List<Vector2> _vertices;

        public Polygon(List<Vector2> vertices) {
            _vertices = vertices;
        }

        public Winding Winding {
            get { 
                var signedDoubleArea = SignedDoubleArea();
                if (signedDoubleArea < 0) {
                    return Winding.Clockwise;
                }
                if (signedDoubleArea > 0) {
                    return Winding.CounterClockwise;
                }
                return Winding.None;
            } 
        }

        private float SignedDoubleArea() {
            var n = _vertices.Count;
            var signedDoubleArea = 0.0f;
            for (int index = 0; index < n; index++) {
                var nextIndex = (index + 1)%n;
                var point = _vertices[index];
                var next = _vertices[nextIndex];
                signedDoubleArea += point.X*next.Y - next.X*point.Y;
            }
            return signedDoubleArea;
        }
        public float Area() {
            return Math.Abs(SignedDoubleArea() * 0.5f);
        }
    }

    public enum Winding {
        Clockwise,
        CounterClockwise,
        None
    }
}