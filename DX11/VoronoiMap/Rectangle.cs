namespace VoronoiMap {
    public class Rectangle {
        public float Left { get; set; }
        public float Top { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public float Right {
            get { return Left + Width; }
        }
        public float Bottom { get { return Top + Height; } }

        public Rectangle(float left, float top, float width, float height) {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
        }
    }
}