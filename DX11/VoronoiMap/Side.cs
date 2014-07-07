namespace VoronoiMap {
    public class Side {
        int Value { get; set; }

        public static readonly Side Left =new Side {Value = 0};
        public static readonly Side Right = new Side { Value = 1 };

        public static implicit operator int(Side s) { return s.Value; }
    }
}