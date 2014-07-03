namespace Fortune.FromJS {
    public class Edge {
        public float A { get; set; }
        public float B { get; set; }
        public float C { get; set; }

        public Site[] Region { get; set; }
        public Site[] Endpoint { get; set; }

        public Edge() {
            Region = new Site[2];
            Endpoint = new Site[2];
        }

        public Edge(Site s1, Site s2) {
            Region = new Site[2];
            Endpoint = new Site[2];
            Region[Side.Left] = s1;
            Region[Side.Right] = s2;
        }
    }
}