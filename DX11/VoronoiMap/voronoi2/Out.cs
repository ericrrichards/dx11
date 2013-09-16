using System;

namespace VoronoiMap.Voronoi2 {
    static class Out {
        public static void Bisector(Edge edge) {
            Console.WriteLine("line({0}) {1}X +{2}Y = {3}, bisecting {4} {5}\n", edge.EdgeID, edge.A, edge.B, edge.C, edge.Sites[LR.Left].SiteID, edge.Sites[LR.Right].SiteID);
        }

        public static void Endpoint(Edge e) {
            Console.WriteLine("e {0} {1} {2}\n", 
                e.EdgeID, 
                e.Vertices[LR.Left] != null ? e.Vertices[LR.Left].SiteID : -1,
                e.Vertices[LR.Right] != null ? e.Vertices[LR.Right].SiteID : -1
                );
        }

        public static void Vertex(Site v) {
            Console.WriteLine("vertex({0}) at {1} {2}\n", v.SiteID, v.Coord.X, v.Coord.Y);
        }

        public static void Site(Site s) {
            Console.WriteLine("site ({0}) at {1} {2}\n", s.SiteID, s.Coord.X, s.Coord.Y);
        }

        public static void Triplet(Site s1, Site s2, Site s3) {
            Console.WriteLine("circle through left={0} right={1} bottom={2}\n", s1.SiteID, s2.SiteID, s3.SiteID);
        }
    }
}