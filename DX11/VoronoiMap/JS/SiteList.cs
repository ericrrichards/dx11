using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Fortune.FromJS {
    public class SiteList {
        public List<Site> Sites { get; set; }
        public Site BottomSite { get; set; }

        public SiteList(IEnumerable<Point> points) {
            Sites = new List<Site>();
            foreach (var point in points) {
                Sites.Add(new Site(point.X, point.Y));
            }
            Sites.Sort();
        }

        public SiteList(int numSites, int width=800, int height=600) {
            var rand = new Random();
            Sites = new List<Site>();
            for (int i = 0; i < numSites; i++) {
                var site = new Site(rand.Next(width), rand.Next(height));
                Sites.Add(site);
            }
            Sites.Sort();
        }

        public Site ExtractMin() {
            var ret = Sites.FirstOrDefault();
            if (ret != null) {
                Sites.RemoveAt(0);
            }
            return ret;
        }
    }
}