using System;
using System.Collections.Generic;
using System.Linq;

namespace Algorithms.Voronoi {
    public static class BoundsCheck {
        [Flags]
        public enum Sides {
            None = 0,
            Top = 1,
            Bottom = 2,
            Left = 4,
            Right = 8
        }

        public static Sides Check(Point p, Rectangle bounds) {
            var value = Sides.None;
            if (Math.Abs(p.X - bounds.Left) < float.Epsilon) {
                value |= Sides.Left;
            }
            if (Math.Abs(p.X - bounds.Right) < float.Epsilon) {
                value|=Sides.Right;
            }
            if (Math.Abs(p.Y - bounds.Top) < float.Epsilon) {
                value |= Sides.Top;
            }
            if (Math.Abs(p.Y - bounds.Bottom) < float.Epsilon) {
                value |= Sides.Bottom;
            }
            return value;
        }
    }

    public class Site : ICoord {
        public const float Epsilon = 0.005f;

        public int Color { get; private set; }
        private float Weight { get; set; }
        public int SiteIndex { get; private set; }
        public List<Edge> Edges { get; private set; }

        public void AddEdge(Edge e) {
            Edges.Add(e);
        }
        private List<LR.Side> EdgeOrientations { get; set; }
        private bool EdgeReordered { get; set; }
        private List<Point> _region;
        
        public Site(Point p, int index, float weight, int color) {
            Init(p, index, weight, color);
        }

        public static Site Create(Point p, int index, float weight, int color) {
            return new Site(p, index, weight, color);
        }

        public static void SortSites(List<Site> sites) {
            sites.Sort(Compare);
        }

        public Edge NearestEdge() {
            Edges.Sort(Edge.CompareSiteDistances);
            return Edges.FirstOrDefault();
        }

        public List<Site> NeighborSites() {
            if (Edges.Count == 0) {
                return new List<Site>();
            }
            if (!EdgeReordered) {
                ReorderEdges();
            }
            return Edges.Select(NeighborSite).ToList();
        }

        public void RegionPrepare(Rectangle clippingBounds) {
            if (EdgeReordered) {
                return;
            }
            ReorderEdges();
            _region = ClipToBounds(clippingBounds);
            if (new Polygon(_region).Winding == WindingDirection.Clockwise) {
                _region.Reverse();
            }
        }

        public List<Point> Region(Rectangle clippingBounds) {
            if (Edges.Count == 0) {
                return new List<Point>();
            }
            RegionPrepare(clippingBounds);
            return _region;
        }
        public override string ToString() {
            return string.Format("site ({0}) at {1} {2}", SiteIndex, X, Y);
        }
        private void Init(Point p, int index, float weight, int color) {
            Coord = p;
            SiteIndex = index;
            Weight = weight;
            Color = color;
            Clear();
        }

        private static int CompareInt(Site s1, Site s2) {
            int returnValue = Voronoi.CompareByYThenX(s1, s2);

            if (returnValue == -1) {
                if (s1.SiteIndex > s2.SiteIndex) {
                    var temp = s2.SiteIndex;
                    s2.SiteIndex = s1.SiteIndex;
                    s1.SiteIndex = temp;
                }
            } else if (returnValue == 1) {
                if (s2.SiteIndex > s1.SiteIndex) {
                    var temp = s2.SiteIndex;
                    s2.SiteIndex = s1.SiteIndex;
                    s1.SiteIndex = temp;
                }
            }
            return returnValue;
        }

        private static int Compare(Site s1, Site s2) {
            return CompareInt(s1, s2);
        }

        private static bool CloseEnough(Point p0, Point p1) {
            return Point.Distance(p0, p1) < Epsilon;
        }

        private void Move(Point p) {
            Clear();
            Coord = p;
        }
        
        private void Clear() {
            Edges = new List<Edge>();
            EdgeOrientations = new List<LR.Side>();
            _region = new List<Point>();
            EdgeReordered = false;
        }

        private Site NeighborSite(Edge edge) {
            if (this == edge.LeftSite) {
                return edge.RightSite;
            }
            if (this == edge.RightSite) {
                return edge.LeftSite;
            }
            return null;
        }

        private void ReorderEdges() {
            var reorderer = new EdgeReorderer(Edges, Criterion.cVertex);
            Edges = reorderer.Edges;
            EdgeOrientations = reorderer.EdgeOrientations;
            EdgeReordered = true;
        }

        private List<Point> ClipToBounds(Rectangle bounds) {
            var points = new List<Point>();
            int i = -1;
            for (int j = 0; j < Edges.Count; j++) {
                var edge = Edges[j];
                if (edge == null || !edge.Visible) {
                    continue;
                }
                if (i >= 0) {
                    Connect(points, j, bounds);
                } else {
                    i = j;
                    var orientation = EdgeOrientations[j];
                    points.Add(edge.ClippedEnds[orientation]);
                    points.Add(edge.ClippedEnds[LR.Other(orientation)]);
                }
            }
            if (i >= 0) {
                Connect(points, i, bounds, true);
            }
            return points;
        }

        private void Connect(List<Point> points, int j, Rectangle bounds, bool closingUp = false) {
            var rightPoint = points.Last();
            var newEdge = Edges[j];
            var newOrientation = EdgeOrientations[j];
            // the point that  must be connected to rightPoint:
            var newPoint = newEdge.ClippedEnds[newOrientation];
            if (!CloseEnough(rightPoint, newPoint)) {
                // The points do not coincide, so they must have been clipped at the bounds;
                // see if they are on the same border of the bounds:
                if (rightPoint.X != newPoint.X && rightPoint.Y != newPoint.Y) {
                    // They are on different borders of the bounds;
                    // insert one or two corners of bounds as needed to hook them up:
                    // (NOTE this will not be correct if the region should take up more than
                    // half of the bounds rect, for then we will have gone the wrong way
                    // around the bounds and included the smaller part rather than the larger)
                    var rightCheck = BoundsCheck.Check(rightPoint, bounds);
                    var newCheck = BoundsCheck.Check(newPoint, bounds);
                    float px, py;

                    // TODO: refactor origin lib copypasta
                    if (rightCheck.HasFlag(BoundsCheck.Sides.Right)) {
                        px = bounds.Right;
                        if (newCheck.HasFlag(BoundsCheck.Sides.Bottom)) {
                            py = bounds.Bottom;
                            points.Add(new Point(px,py));
                        } else if (newCheck.HasFlag(BoundsCheck.Sides.Top)) {
                            py = bounds.Top;
                            points.Add(new Point(px,py));
                        } else if (newCheck.HasFlag(BoundsCheck.Sides.Left)) {
                            if (rightPoint.Y - bounds.Y + newPoint.Y - bounds.Y < bounds.Height) {
                                py = bounds.Top;
                            } else {
                                py = bounds.Bottom;
                            }
                            points.Add(new Point(px,py));
                            points.Add(new Point(bounds.Left, py));
                        }
                    } else if (rightCheck.HasFlag(BoundsCheck.Sides.Left)) {
                        px = bounds.Left;
                        if (newCheck.HasFlag(BoundsCheck.Sides.Bottom)) {
                            py = bounds.Bottom;
                            points.Add(new Point(px, py));
                        } else if (newCheck.HasFlag(BoundsCheck.Sides.Top)) {
                            py = bounds.Top;
                            points.Add(new Point(px, py));
                        } else if (newCheck.HasFlag(BoundsCheck.Sides.Right)) {
                            if (rightPoint.Y - bounds.Y + newPoint.Y - bounds.Y < bounds.Height) {
                                py = bounds.Top;
                            } else {
                                py = bounds.Bottom;
                            }
                            points.Add(new Point(px, py));
                            points.Add(new Point(bounds.Right, py));
                        }
                    } else if (rightCheck.HasFlag(BoundsCheck.Sides.Top)) {
                        py = bounds.Top;
                        if (newCheck.HasFlag(BoundsCheck.Sides.Right)) {
                            px = bounds.Right;
                            points.Add(new Point(px, py));
                        } else if (newCheck.HasFlag(BoundsCheck.Sides.Left)) {
                            px = bounds.Left;
                            points.Add(new Point(px, py));
                        } else if (newCheck.HasFlag(BoundsCheck.Sides.Bottom)) {
                            if (rightPoint.X - bounds.X + newPoint.X - bounds.Y < bounds.Width) {
                                px = bounds.Left;
                            } else {
                                px = bounds.Right;
                            }
                            points.Add(new Point(px, py));
                            points.Add(new Point(bounds.Left, bounds.Bottom));
                        }
                    } else if (rightCheck.HasFlag(BoundsCheck.Sides.Bottom)) {
                        py = bounds.Bottom;
                        if (newCheck.HasFlag(BoundsCheck.Sides.Right)) {
                            px = bounds.Right;
                            points.Add(new Point(px, py));
                        } else if (newCheck.HasFlag(BoundsCheck.Sides.Left)) {
                            px = bounds.Left;
                            points.Add(new Point(px, py));
                        } else if (newCheck.HasFlag(BoundsCheck.Sides.Bottom)) {
                            if (rightPoint.X - bounds.X + newPoint.X - bounds.Y < bounds.Width) {
                                px = bounds.Left;
                            } else {
                                px = bounds.Right;
                            }
                            points.Add(new Point(px, py));
                            points.Add(new Point(bounds.Left, bounds.Top));
                        }
                    }
                }
                if (closingUp) {
                    // newEdge's ends have already been added
                    return;
                }
                points.Add(newPoint);
            }
            var newRightPoint = newEdge.ClippedEnds[LR.Other(newOrientation)];
            if (!CloseEnough(points.First(), newRightPoint)) {
                points.Add(newRightPoint);
            }
        }

    }
}