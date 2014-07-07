using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using log4net;

namespace VoronoiMap {
    public class SiteList {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private List<Site> Sites { get; set; }
        public Site BottomSite { get; set; }

        public SiteList(IEnumerable<Point> points) {
            Site.ResetSiteCount();
            Sites = new List<Site>();
            var sites = new HashSet<Site>();
            foreach (var point in points) {
                sites.Add(new Site(point.X, point.Y));
            }
            Sites = sites.ToList();
            Sites.Sort();
        }

        public Site ExtractMin() {
            var ret = Sites.FirstOrDefault();
            if (ret != null) {
                Sites.RemoveAt(0);
            }
            return ret;
        }

        public void LogSites() {
            var sb = new StringBuilder();
            sb.AppendLine("==============================");
            sb.AppendLine("Sites:");
            foreach (var site in Sites) {
               sb.AppendLine(string.Format("\t{0}", site));
            }
            sb.AppendLine("==============================");
            Log.Info(sb.ToString());
        }
    }
}