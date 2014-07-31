using System.Collections.Generic;
using System.Linq;

namespace Algorithms.Voronoi {
    internal class SiteList {
        private readonly List<Site> _sites;
        private int _currentIndex;
        private bool _sorted;

        public SiteList() {
            _sites = new List<Site>();
            _currentIndex = 0;
            _sorted = false;
        }

        public int Push(Site site) {
            _sorted = false;
            _sites.Add(site);
            return Length;
        }
        public int Length { get { return _sites.Count; } }

        public Site Next() {
            if (_sorted && _currentIndex < Length) {
                return _sites[_currentIndex++];
            }
            return null;
        }

        public Rectangle SiteBounds() {
            if (!_sorted) {
                Site.SortSites(_sites);
                _currentIndex = 0;
                _sorted = true;
            }
            if (_sites.Count == 0) {
                return new Rectangle(0, 0, 0, 0);
            }
            var xMax = _sites.Max(s => s.X);
            var xMin = _sites.Min(s => s.X);
            // here's where we assume that the sites have been sorted on y:
            var yMin = _sites.First().Y;
            var yMax = _sites.Last().Y;

            return new Rectangle(xMin, yMin, xMax-xMin, yMax-yMin);
        }

        public List<int> SiteColors() {return _sites.Select(s => s.Color).ToList();}
        public List<Point> SiteCoords() {return _sites.Select(s => s.Coord).ToList();}

        public List<Circle> Circles() {
            var circles = new List<Circle>();
            foreach (var site in _sites) {
                var radius = 0.0f;
                var nearestEdge = site.NearestEdge();
                if (!nearestEdge.IsPartOfConvexHull) {
                    radius = nearestEdge.SiteDistance()*0.5f;
                }
                circles.Add(new Circle(site.X, site.Y, radius));
            }
            return circles;
        }

        public List<List<Point>> Regions(Rectangle bounds) {
            return _sites.Select(site => site.Region(bounds)).ToList();
        }

        public void RegionsPrepare(Rectangle bounds) {
            foreach (var site in _sites) {
                site.RegionPrepare(bounds);
            }
        }

        public Point NearestSitePoint(float x, float y) {
            return null;
        }
    }
}