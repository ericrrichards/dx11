using SlimDX;

namespace VoronoiMap.Voronoi2 {
    using System.Linq;

    public class PriorityQueue {
        private int _min;
        private int _count;
        private int _hashSize;
        private HalfEdge[] _hash;

        public void Insert(HalfEdge he, Site v, float offset) {
            he.Vertex = v;
            he.YStar = v.Coord.Y + offset;
            var last = _hash[Bucket(he)];
            HalfEdge next;
            while ((next = last.PriorityQueueNext) != null && (he.YStar > next.YStar || he.YStar == next.YStar && v.Coord.X > next.Vertex.Coord.X)) {
                last = next;
            }
            he.PriorityQueueNext = last.PriorityQueueNext;
            last.PriorityQueueNext = he;
            _count++;
        }
        public void Delete(HalfEdge he) {
            if (he.Vertex != null) {
                HalfEdge last = _hash[Bucket(he)];
                while (last.PriorityQueueNext != he) {
                    last = last.PriorityQueueNext;
                }
                last.PriorityQueueNext = he.PriorityQueueNext;
                _count--;
                he.Vertex = null;
            }
        }
        public int Bucket(HalfEdge he) {
            int bucket;
            if (he.YStar < Geometry.Bounds.Top) bucket = 0;
            else if (he.YStar >= Geometry.Bounds.Bottom) bucket = _hashSize - 1;
            else bucket = (int) ((he.YStar - Geometry.Bounds.Top)/Geometry.DeltaY*_hashSize);
            if (bucket < 0) {
                bucket = 0;
            }
            if (bucket >= _hashSize) {
                bucket = _hashSize - 1;
            }
            if (bucket < _min) {
                _min = bucket;
            }
            return bucket;

        }
        public bool Empty { get { return _count == 0; } }
        public Vector2 Min() {
            Vector2 answer;
            while (_hash[_min].PriorityQueueNext == null) {
                _min++;
            }
            answer.X = _hash[_min].PriorityQueueNext.Vertex.Coord.X;
            answer.Y = _hash[_min].PriorityQueueNext.YStar;
            return answer;
        }
        public HalfEdge ExtractMin() {
            HalfEdge curr = _hash[_min].PriorityQueueNext;
            _hash[_min].PriorityQueueNext = curr.PriorityQueueNext;
            _count--;
            return curr;
        }
        public PriorityQueue(int sqrtNSites) {
            _count = _min = 0;
            _hashSize = 4*sqrtNSites;
            _hash = Enumerable.Repeat(new HalfEdge(), _hashSize).ToArray();
            for (int i = 0; i < _hashSize; i++) {
                _hash[i].PriorityQueueNext = null;
            }
        }
    }
}