namespace Algorithms.Voronoi {
    public static class LR {
        public enum Side {
            Left = 0,
            Right = 1
        }

        public static Side Other(Side leftRight) {
            return leftRight == Side.Left ? Side.Right : Side.Left;
        }
    }
}