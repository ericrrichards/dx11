using System;
using System.Collections.Generic;

namespace VoronoiMap {
    public class EventQueue {
        private readonly List<HalfEdge> _events = new List<HalfEdge>();

        public void Insert(HalfEdge he, Site site, float offset) {
            he.Vertex = site;
            he.YStar = site.Y + offset;
            int i;
            for (i = 0; i < _events.Count; i++) {
                var next = _events[i];
                if (he.YStar > next.YStar || (Math.Abs(he.YStar - next.YStar) < Geometry.Tolerance && site.X > next.Vertex.X)) {
                    continue;
                }
                break;
            }
            _events.Insert(i, he);
        }

        public void Delete(HalfEdge he) {
            _events.Remove(he);
        }
        public bool IsEmpty { get { return _events.Count == 0; } }

        public Site Min() {
            var elem = _events[0];
            return new Site(elem.Vertex.X, elem.YStar);
        }

        public HalfEdge ExtractMin() {
            var ret = _events[0];
            _events.RemoveAt(0);
            return ret;
        }
    }
}