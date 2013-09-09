using System.Collections.Generic;
using SlimDX;

namespace VoronoiMap.Voronoi {
    class HalfEdgePriorityQueue {
        private List<HalfEdge> _hash;
        private int _count;
        private int _minBucket;
        private int _hashSize;

        private float _ymin;
        private float _deltaY;


        public HalfEdgePriorityQueue(float ymin, float deltay, int sqrtNSites) {
            _ymin = ymin;
            _deltaY = deltay;
            _hashSize = 4*sqrtNSites;
            Initialize();
        }

        public bool Empty { get { return _count == 0; } }
        public Vector2 Min() {
            AdjustMinBucket();
            var answer = _hash[_minBucket].NextInPriorityQueue;
            return new Vector2(answer.Vertex.X, answer.YStar);
        }

        private void AdjustMinBucket() {
            while (_minBucket < _hashSize - 1 && IsEmpty(_minBucket)) {
                ++_minBucket;
            }
        }

        private bool IsEmpty(int bucket) {
            return _hash[bucket].NextInPriorityQueue == null;
        }

        private void Initialize() {
            _count = 0;
            _minBucket = 0;
            _hash = new List<HalfEdge>(new HalfEdge[_hashSize]);
            for (int i = 0; i < _hashSize; i++) {
                _hash[i] = new HalfEdge {
                    NextInPriorityQueue = null
                };
            }
        }

        public void Remove(HalfEdge halfEdge) {
            var removalBucket = Bucket(halfEdge);
            if (halfEdge.Vertex != null) {
                var previous = _hash[removalBucket];
                while (previous.NextInPriorityQueue != halfEdge) {
                    previous = previous.NextInPriorityQueue;
                }
                previous.NextInPriorityQueue = halfEdge.NextInPriorityQueue;
                _count--;
            }
        }

        private int Bucket(HalfEdge halfEdge) {
            var theBucket = (int)((halfEdge.YStar - _ymin)/_deltaY*_hashSize);
            if (theBucket < 0) theBucket = 0;
            if (theBucket >= _hashSize) theBucket = _hashSize - 1;
            return theBucket;
        }

        public void Insert(HalfEdge halfEdge) {
            var insertionBucket = Bucket(halfEdge);
            if (insertionBucket < _minBucket) {
                _minBucket = insertionBucket;
            }
            var previous = _hash[insertionBucket];
            HalfEdge next;
            while ((next = previous.NextInPriorityQueue) != null && (halfEdge.YStar > next.YStar && halfEdge.Vertex.X > next.Vertex.X)) {
                previous = next;
            }
            halfEdge.NextInPriorityQueue = previous.NextInPriorityQueue;
            previous.NextInPriorityQueue = halfEdge;
            ++_count;
        }

        public HalfEdge ExtractMin() {
            var answer = _hash[_minBucket].NextInPriorityQueue;
            _hash[_minBucket].NextInPriorityQueue = answer.NextInPriorityQueue;
            _count--;
            answer.NextInPriorityQueue = null;
            return answer;
        }
    }
}