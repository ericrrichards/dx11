using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Core;

namespace VoronoiMap {
    /// <summary>
    /// Adapted from http://philogb.github.io/blog/assets/voronoijs/voronoi.html
    /// </summary>


    public class Site : IEquatable<Site>, IComparable<Site> {
        private static int _siteCount;
        public static void ResetSiteCount() { _siteCount = 0; }

        public float X { get; private set; }
        public float Y { get; private set; }
        private int SiteNum { get; set; }
        public bool New { get; set; }
        public List<Edge> Edges { get; private set; }
        private List<Side> EdgeOrientations { get; set; }
        private bool EdgeReordered { get; set; }
        private List<Site> _region;

        public Site(float x, float y) {
            X = x;
            Y = y;
            SiteNum = _siteCount++;
            Edges = new List<Edge>();
        }

        public Site(PointF p) : this(p.X, p.Y) { }

        public void AddEdge(Edge e) {
            Edges.Add(e);
        }
        public Edge NearestEdge() {
            Edges.Sort(Edge.CompareSiteDistances);
            return Edges.FirstOrDefault();
        }
        private Site NeighborSite(Edge edge) {
            if (this == edge.Region[Side.Left]) {
                return edge.Region[Side.Right];
            }
            if (this == edge.Region[Side.Right]) {
                return edge.Region[Side.Left];
            }
            return null;
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
        private void ReorderEdges() {
            var reorderer = new EdgeReorderer(Edges, Criterion.Vertex);
            Edges = reorderer.Edges;
            EdgeOrientations = reorderer.EdgeOrientations;
            EdgeReordered = true;
        }

        public static float Distance(Site s1, Site s2) {
            var dx = s1.X - s2.X;
            var dy = s1.Y - s2.Y;
            return MathF.Sqrt(dx * dx + dy * dy);
        }
        
        public List<Site> Region(RectangleF clippingBounds) {
            if (Edges.Count == 0) {
                return new List<Site>();
            }
            RegionPrepare(clippingBounds);
            return _region;
        }

        private void RegionPrepare(RectangleF clippingBounds) {
            if (EdgeReordered) {
                return;
            }
            ReorderEdges();
            _region = ClipToBounds(clippingBounds);
            if (new Polygon(_region).Winding == WindingDirection.Clockwise) {
                _region.Reverse();
            }
        }
        private static bool CloseEnough(Site p0, Site p1) {
            return Distance(p0, p1) < .005f;
        }
        private List<Site> ClipToBounds(RectangleF bounds) {
            var points = new List<Site>();
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
                    points.Add(edge.ClippedEndpoints[orientation]);
                    points.Add(edge.ClippedEndpoints[Side.Other(orientation)]);
                }
            }
            if (i >= 0) {
                Connect(points, i, bounds, true);
            }
            return points;
        }
        private void Connect(List<Site> points, int j, RectangleF bounds, bool closingUp = false) {
            var rightPoint = points.Last();
            var newEdge = Edges[j];
            var newOrientation = EdgeOrientations[j];
            // the point that  must be connected to rightPoint:
            var newPoint = newEdge.ClippedEndpoints[newOrientation];
            if (!CloseEnough(rightPoint, newPoint)) {
                // The points do not coincide, so they must have been clipped at the bounds;
                // see if they are on the same border of the bounds:
                if (Math.Abs(rightPoint.X - newPoint.X) > Geometry.Tolerance && Math.Abs(rightPoint.Y - newPoint.Y) > Geometry.Tolerance) {
                    // They are on different borders of the bounds;
                    // insert one or two corners of bounds as needed to hook them up:
                    // (NOTE this will not be correct if the region should take up more than
                    // half of the bounds rect, for then we will have gone the wrong way
                    // around the bounds and included the smaller part rather than the larger)
                    var rightCheck = BoundsCheck.Check(rightPoint, bounds);
                    var newCheck = BoundsCheck.Check(newPoint, bounds);
                    
                    if (rightCheck.HasFlag(BoundsCheck.Sides.Right)) {
                        if (newCheck.HasFlag(BoundsCheck.Sides.Bottom)) {
                            points.Add(new Site(bounds.Right, bounds.Bottom));
                        } else if (newCheck.HasFlag(BoundsCheck.Sides.Top)) {
                            points.Add(new Site(bounds.Right, bounds.Top));
                        } else if (newCheck.HasFlag(BoundsCheck.Sides.Left)) {
                            var py = ((rightPoint.Y - bounds.Y + newPoint.Y - bounds.Y) < bounds.Height) ? bounds.Top : bounds.Bottom;
                            points.Add(new Site(bounds.Right, py));
                            points.Add(new Site(bounds.Left, py));
                        }
                    } else if (rightCheck.HasFlag(BoundsCheck.Sides.Left)) {
                        if (newCheck.HasFlag(BoundsCheck.Sides.Bottom)) {
                            points.Add(new Site(bounds.Left, bounds.Bottom));
                        } else if (newCheck.HasFlag(BoundsCheck.Sides.Top)) {
                            points.Add(new Site(bounds.Left, bounds.Top));
                        } else if (newCheck.HasFlag(BoundsCheck.Sides.Right)) {
                            var py = ((rightPoint.Y - bounds.Y + newPoint.Y - bounds.Y) < bounds.Height) ? bounds.Top : bounds.Bottom;
                            points.Add(new Site(bounds.Left, py));
                            points.Add(new Site(bounds.Right, py));
                        }
                    } else if (rightCheck.HasFlag(BoundsCheck.Sides.Top)) {
                        if (newCheck.HasFlag(BoundsCheck.Sides.Right)) {
                            points.Add(new Site(bounds.Right, bounds.Top));
                        } else if (newCheck.HasFlag(BoundsCheck.Sides.Left)) {
                            points.Add(new Site(bounds.Left, bounds.Top));
                        } else if (newCheck.HasFlag(BoundsCheck.Sides.Bottom)) {
                            var px = ((rightPoint.X - bounds.X + newPoint.X - bounds.X) < bounds.Width) ? bounds.Left : bounds.Right;
                            points.Add(new Site(px, bounds.Top));
                            points.Add(new Site(bounds.Left, bounds.Bottom));
                        }
                    } else if (rightCheck.HasFlag(BoundsCheck.Sides.Bottom)) {
                        if (newCheck.HasFlag(BoundsCheck.Sides.Right)) {
                            points.Add(new Site(bounds.Right, bounds.Bottom));
                        } else if (newCheck.HasFlag(BoundsCheck.Sides.Left)) {
                            points.Add(new Site(bounds.Left, bounds.Bottom));
                        } else if (newCheck.HasFlag(BoundsCheck.Sides.Bottom)) {
                            var px = ((rightPoint.X - bounds.X + newPoint.X - bounds.X) < bounds.Width) ? bounds.Left : bounds.Right;
                            points.Add(new Site(px, bounds.Bottom));
                            points.Add(new Site(bounds.Left, bounds.Top));
                        }
                    }
                }
                if (closingUp) {
                    // newEdge's ends have already been added
                    return;
                }
                points.Add(newPoint);
            }
            var newRightPoint = newEdge.ClippedEndpoints[Side.Other(newOrientation)];
            if (!CloseEnough(points.First(), newRightPoint)) {
                points.Add(newRightPoint);
            }
        }

        #region Equality and comparison stuff
        public bool Equals(Site other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Site) obj);
        }

        public override int GetHashCode() {
            unchecked {
                return (X.GetHashCode()*397) ^ Y.GetHashCode();
            }
        }

        public static bool operator ==(Site left, Site right) { return Equals(left, right); }
        public static bool operator !=(Site left, Site right) { return !Equals(left, right); }



        public int CompareTo(Site other) {
            if (other == null) return 1;
            if (Y < other.Y) return -1;
            if (Y > other.Y) return 1;
            if (X < other.X) return -1;
            if (X > other.X) return 1;
            return 0;
        }

        public static implicit operator Point(Site p) {
            return new Point((int)p.X, (int)p.Y);
        }
        public static implicit operator PointF(Site p) {
            return new PointF((int)p.X, (int)p.Y);
        }

        public override string ToString() { return String.Format("[#{2} {0},{1}]", (int)X, (int)Y, SiteNum); }
        #endregion

        public static Site CreateIntersectingSite(HalfEdge el1, HalfEdge el2) {
            var e1 = el1.Edge;
            var e2 = el2.Edge;
            if (e1 == null || e2 == null) {
                return null;
            }
            if (e1.Region[Side.Right] == e2.Region[Side.Right]) {
                return null;
            }
            var d = (e1.A*e2.B) - (e1.B*e2.A);
            if (Math.Abs(d) < Geometry.Tolerance) {
                return null;
            }
            var xint = (e1.C*e2.B - e2.C*e1.B)/d;
            var yint = (e2.C*e1.A - e1.C*e2.A)/d;

            var e1Region = e1.Region[Side.Right];
            var e2Region = e2.Region[Side.Right];

            HalfEdge el;
            Edge e;
            if ((e1Region.Y < e2Region.Y) || Math.Abs(e1Region.Y - e2Region.Y) < Geometry.Tolerance && e1Region.X < e2Region.X) {
                el = el1;
                e = e1;
            } else {
                el = el2;
                e = e2;
            }
            var rightOfSite = (xint >= e.Region[Side.Right].X);
            if ((rightOfSite && (el.Side == Side.Left)) || (!rightOfSite && (el.Side == Side.Right))) {
                return null;
            }

            var vertex = new Site(xint, yint);
            //vertex.AddEdge(e1);
            //vertex.AddEdge(e2);
            return vertex;
        }
    }
}
