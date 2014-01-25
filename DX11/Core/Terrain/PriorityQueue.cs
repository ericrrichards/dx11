using System;
using System.Collections;
using System.Collections.Generic;

namespace Core.Terrain {
    public abstract class PriorityQueueNode {
        public float Priority { get; set; }
        internal long InsertionIndex { get; set; }
        internal int QueueIndex { get; set; }
    }

    /// <summary>
    /// https://bitbucket.org/BlueRaja/high-speed-priority-queue-for-c/overview
    /// </summary>
    /// <typeparam name="T"></typeparam>
    class PriorityQueue<T> : IEnumerable<T> where T : PriorityQueueNode {
        private readonly T[] _nodes;
        private long _numNodesEverEnqueued;

        public PriorityQueue(int maxNodes) {
            Count = 0;
            _nodes = new T[maxNodes+1];
            _numNodesEverEnqueued = 0;
        }

        public void Remove(T node) {
            if (Count <= 1) {
                _nodes[1] = null;
                Count = 0;
                return;
            }
            var wasSwapped = false;
            var formerLastNode = _nodes[Count];
            if (node.QueueIndex != Count) {
                Swap(node, formerLastNode);
                wasSwapped = true;
            }
            Count--;
            _nodes[node.QueueIndex] = null;
            if (wasSwapped) {
                OnNodeUpdated(formerLastNode);
            }
        }

        public void UpdatePriority(T node, float priority) {
            node.Priority = priority;
            OnNodeUpdated(node);

        }

        private void OnNodeUpdated(T node) {
            var parentIndex = node.QueueIndex/2;
            var parentNode = _nodes[parentIndex];
            if (parentIndex > 0 && HasHigherPriority(node, parentNode)) {
                CascadeUp(node);
            } else {
                CascadeDown(node);
            }
        }

        public void Enqueue(T node, float priority) {
            node.Priority = priority;
            Count++;
            _nodes[Count] = node;
            node.QueueIndex = Count;
            node.InsertionIndex = _numNodesEverEnqueued++;
            CascadeUp(_nodes[Count]);
        }

        private void CascadeUp(T node) {
            var parent = node.QueueIndex/2;
            while (parent >= 1) {
                var parentNode = _nodes[parent];
                if (HasHigherPriority(parentNode, node)) {
                    break;
                }

                Swap(node, parentNode);
                parent = node.QueueIndex/2;
            }
        }

        private void CascadeDown(T node) {
            var finalQueueIndex = node.QueueIndex;
            while (true) {
                var newParent = node;
                var childLeftIndex = 2*finalQueueIndex;
                if (childLeftIndex > Count) {
                    node.QueueIndex = finalQueueIndex;
                    _nodes[finalQueueIndex] = node;
                    break;
                }
                var childLeft = _nodes[childLeftIndex];
                if (HasHigherPriority(childLeft, newParent)) {
                    newParent = childLeft;
                }
                var childRightIndex = childLeftIndex + 1;
                if (childRightIndex <= Count) {
                    var childRight = _nodes[childRightIndex];
                    if (HasHigherPriority(childRight, newParent)) {
                        newParent = childRight;
                    }
                }
                if (newParent != node) {
                    _nodes[finalQueueIndex] = newParent;
                    var temp = newParent.QueueIndex;
                    newParent.QueueIndex = finalQueueIndex;
                    finalQueueIndex = temp;
                } else {
                    node.QueueIndex = finalQueueIndex;
                    _nodes[finalQueueIndex] = node;
                    break;
                }
            }
        }


        private void Swap(T node1, T node2) {
            _nodes[node1.QueueIndex] = node2;
            _nodes[node2.QueueIndex] = node1;
            var temp = node1.QueueIndex;
            node1.QueueIndex = node2.QueueIndex;
            node2.QueueIndex = temp;
        }

        private static bool HasHigherPriority(T higher, T lower) {
            return higher.Priority < lower.Priority || (Math.Abs(higher.Priority - lower.Priority) < float.Epsilon && higher.InsertionIndex < lower.InsertionIndex);
        }

        public T Dequeue() {
            var ret = _nodes[1];
            Remove(ret);
            return ret;
        }

        public T First {
            get { return _nodes[1]; }
        }

        public int MaxSize { get { return _nodes.Length - 1; } }

        public void Clear() {
            for (var i = 0; i < _nodes.Length; i++) {
                _nodes[i] = null;
            }
            Count = 0;
        }

        public bool Contains(T node) { return _nodes[node.QueueIndex] == node; }


        public int Count { get; private set; }


        public IEnumerator<T> GetEnumerator() {
            for (int i = 1 ; i <= Count; i++) {
                yield return _nodes[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
    }
}
