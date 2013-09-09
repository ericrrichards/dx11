using System;
using System.Collections.Generic;
using System.Linq;

namespace VoronoiMap.Voronoi {
    class SiteList {
        private List<Site> _sites;
        private int _currentIndex;
        private bool _sorted;

        public SiteList() {
            _sites = new List<Site>();
            _sorted = false;
        }

        public int Length {
            get { return _sites.Count; }
        }

        public void Push(Site site) {
            _sorted = false;
            _sites.Add(site);
        }

        public Rectangle GetSiteBounds() {
            if (!_sorted) {
                _sites.Sort(new Site.SiteComparer());
                _currentIndex = 0;
                _sorted = true;
            }
            if (_sites.Count == 0) {
                return new Rectangle(0,0,0,0);
            }
            var xmin = float.MaxValue;
            var xmax = float.MinValue;
            foreach (var site in _sites) {
                xmin = Math.Min(xmin, site.X);
                xmax = Math.Max(xmax, site.X);
            }
            var ymin = _sites.First().Y;
            var ymax = _sites.Last().Y;

            return new Rectangle(xmin, ymin, xmax-xmin, ymax-ymin);
        }

        public Site Next() {
            if (_sorted == false) {
                throw new Exception("SiteList.Next() - sites have not been sorted");
            }
            if (_currentIndex < _sites.Count) {
                return _sites[_currentIndex++];
            } else {
                return null;
            }
        }
    }
}