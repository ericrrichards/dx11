using System;
using System.Collections.Generic;

namespace VoronoiMap {
    public class EventQueue {
        public List<HalfEdge> Events = new List<HalfEdge>();

        public void Insert(HalfEdge he, Site site, float offset) {
            he.Vertex = site;
            he.YStar = site.Y + offset;
            int i;
            for (i = 0; i < Events.Count; i++) {
                var next = Events[i];
                if (he.YStar > next.YStar || (Math.Abs(he.YStar - next.YStar) < Geometry.Tolerance && site.X > next.Vertex.X)) {
                    continue;
                }
                break;
            }
            Events.Insert(i, he);
        }

        public void Delete(HalfEdge he) {
            Events.Remove(he);
        }
        public bool IsEmpty { get { return Events.Count == 0; } }

        public HalfEdge NextEvent(HalfEdge he) {
            for (int i = 0; i < Events.Count; i++) {
                if (Events[i] == he) {
                    if (i < Events.Count-1) {
                        return Events[i + 1];
                    }
                    return null;
                }
            }
            return null;
        }

        public Site Min() {
            var elem = Events[0];
            return new Site(elem.Vertex.X, elem.YStar);
        }

        public HalfEdge ExtractMin() {
            var ret = Events[0];
            Events.RemoveAt(0);
            return ret;
        }
    }
}