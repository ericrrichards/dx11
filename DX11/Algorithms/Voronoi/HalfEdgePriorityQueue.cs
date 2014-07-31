using System.Collections.Generic;

namespace Algorithms.Voronoi {
    internal class HalfEdgePriorityQueue {
        private List<HalfEdge> _hash;
        private int _count;
        private int _minBucket;
        private int _hashSize;
        private float _ymin;
        private float _deltay;


        public HalfEdgePriorityQueue(float ymin, float deltay, int sqrtNSites) {
            _ymin = ymin;
            _deltay = deltay;
            _hashSize = 4*sqrtNSites;
            Initialize();
        }

        public void Insert(HalfEdge he) {
            var insertBucket = Bucket(he);
            if (insertBucket < _minBucket) {
                _minBucket = insertBucket;
            }
            var previous = _hash[insertBucket];
            var next = previous.NextInPriorityQueue;
            while ((next = previous.NextInPriorityQueue) != null && (he.YStar > next.YStar || (he.YStar == next.YStar && he.Vertex.X>next.Vertex.X))) {
                previous = next;
            }
            he.NextInPriorityQueue = previous.NextInPriorityQueue;
            previous.NextInPriorityQueue = he;
            _count++;

        }

        public void Remove(HalfEdge he) {
            var removalBucket = Bucket(he);
            if (he.Vertex != null) {
                var previous = _hash[removalBucket];
                while (previous.NextInPriorityQueue != he) {
                    previous = previous.NextInPriorityQueue;
                }
                previous.NextInPriorityQueue = he.NextInPriorityQueue;
                _count--;
                he.Vertex = null;
                he.NextInPriorityQueue = null;
                
            }
        }
        public bool Empty { get { return _count == 0; } }

        public Point Min() {
            AdjustMinBucket();
            var answer = _hash[_minBucket].NextInPriorityQueue;
            return new Point(answer.Vertex.X, answer.YStar);
        }

        public HalfEdge ExtractMin() {
            var answer = _hash[_minBucket].NextInPriorityQueue;
            _hash[_minBucket].NextInPriorityQueue = answer.NextInPriorityQueue;
            _count--;
            answer.NextInPriorityQueue = null;
            return answer;
        }

        private void Initialize() {
            _count = 0;
            _minBucket = 0;
            _hash = new List<HalfEdge>(_hashSize);
            for (int i = 0; i < _hashSize; i++) {
                _hash.Add(HalfEdge.CreateDummy());
                _hash[i].NextInPriorityQueue = null;
            }
        }

        private int Bucket(HalfEdge he) {
            var theBucket = (int) ((he.YStar - _ymin)/_deltay*_hashSize);
            if (theBucket < 0) {
                theBucket = 0;
            }
            if (theBucket >= _hashSize) {
                theBucket = _hashSize - 1;
            }
            return theBucket;
        }

        private bool IsEmpty(int bucket) {
            return (_hash[bucket].NextInPriorityQueue == null);
        }

        private void AdjustMinBucket() {
            while (_minBucket < _hashSize - 1 && IsEmpty(_minBucket)) {
                ++_minBucket;
            }
        }
    }
}