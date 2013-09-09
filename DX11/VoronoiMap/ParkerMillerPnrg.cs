namespace VoronoiMap {
    public class ParkerMillerPnrg {
        public int Seed { get; set; }
        public ParkerMillerPnrg() {
            Seed = 1;
        }
        public int NextInt() {
            return Gen();
        }
        
        public float NextFloat() {
            return Gen()/2147483647.0f;
        }
        public float NextFloatRange(float min, float max) {
            return min + ((max - min)*NextFloat());
        }
        private int Gen() {
            return Seed = (Seed * 16807) % 2147483647;
        }
        
    }
}