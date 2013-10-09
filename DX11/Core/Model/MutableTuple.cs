namespace Core.Model {
    public class MutableTuple<T1, T2, T3> {
        public MutableTuple(T1 i, T2 i1, T3 i2) {
            Item1 = i;
            Item2 = i1;
            Item3 = i2;
        }

        public T1 Item1 { get; set; }
        public T2 Item2 { get; set; }
        public T3 Item3 { get; set; }
    }
}