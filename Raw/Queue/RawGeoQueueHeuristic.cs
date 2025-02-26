using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Ces.Collections
{
    [NoAlias]
    public unsafe struct RawGeoQueueHeuristic<T, H>
        where T : unmanaged, IEquatable<T>
        where H : unmanaged, IRawHeuristicable<T>
    {
        [NativeDisableUnsafePtrRestriction, NoAlias]
        T* _heap;

        UnsafeHashMap<T, double> _costs;
        UnsafeHashMap<T, int> _indexInHeap;
        int _count;
        int _capacity;

        H _heuristic;

        readonly Allocator _allocator;

        public readonly int Count => _count;
        public readonly bool IsCreated => _heap != null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RawGeoQueueHeuristic(int capacity, Allocator allocator)
        {
            _heap = null;
            _costs = new UnsafeHashMap<T, double>(capacity, allocator);
            _indexInHeap = new UnsafeHashMap<T, int>(capacity, allocator);

            _count = 0;
            _capacity = 0;
            _heuristic = default;
            _allocator = allocator;

            SetCapacity(capacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item, double cost)
        {
            ResizeIfFull();

            _heap[_count++] = item;
            _costs[item] = cost;
            _indexInHeap[item] = _count - 1;

            HeapifyUp(_count - 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddOrUpdate(T item, double cost)
        {
            if (_indexInHeap.TryGetValue(item, out int index))
            {
                _costs[item] = cost;
                HeapifyUp(index);
                return;
            }

            Add(item, cost);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Pop()
        {
            var root = _heap[0];

            _heap[0] = _heap[_count - 1];
            _indexInHeap[_heap[0]] = 0;

            _count--;
            _indexInHeap.Remove(root);

            HeapifyDown(0);

            return root;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPop(out T item)
        {
            if (Count == 0)
            {
                item = default;
                return false;
            }

            item = Pop();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            _costs.Clear();
            _indexInHeap.Clear();

            _count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Initialize(H heuristic)
        {
            _heuristic = heuristic;

            Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double GetCost(T item)
        {
            return _costs[item];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetCost(T item, out double cost)
        {
            return _costs.TryGetValue(item, out cost);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (!IsCreated)
            {
#if CES_COLLECTIONS_WARNING
                Debug.LogError("RawGeoQueue :: Dispose :: Is not created!");
#endif
                return;
            }

            CesMemoryUtility.FreeAndNullify(ref _heap, _allocator);

            _costs.Dispose();
            _indexInHeap.Dispose();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        void HeapifyUp(int index)
        {
            if (index < 0 || index > _count - 1)
                return;

            var item = _heap[index];

            while (index > 0)
            {
                int parentIndex = (index - 1) / 2;
                var parentItem = _heap[parentIndex];

                if (IsLowerCost(item, parentItem))
                {
                    _indexInHeap[item] = parentIndex;
                    _indexInHeap[parentItem] = index;

                    (_heap[index], _heap[parentIndex]) = (_heap[parentIndex], _heap[index]);

                    index = parentIndex;
                }
                else
                {
                    break;
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        void HeapifyDown(int index)
        {
            int lastIndex = _count - 1;

            while (true)
            {
                int leftChildIndex = 2 * index + 1;
                int rightChildIndex = 2 * index + 2;
                int smallest = index;

                if (leftChildIndex <= lastIndex && IsLowerCost(_heap[leftChildIndex], _heap[smallest]))
                {
                    smallest = leftChildIndex;
                }

                if (rightChildIndex <= lastIndex && IsLowerCost(_heap[rightChildIndex], _heap[smallest]))
                {
                    smallest = rightChildIndex;
                }

                if (smallest == index)
                    break;

                _indexInHeap[_heap[index]] = smallest;
                _indexInHeap[_heap[smallest]] = index;

                (_heap[index], _heap[smallest]) = (_heap[smallest], _heap[index]);
                index = smallest;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IsLowerCost(T lhs, T rhs)
        {
            return _heuristic.CalculateHeuristic(lhs) + _costs[lhs] < _heuristic.CalculateHeuristic(rhs) + _costs[rhs];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        void SetCapacity(int capacity)
        {
#if CES_COLLECTIONS_CHECK
            if (capacity <= _capacity)
                throw new Exception($"RawGeoQueue :: SetCapacity :: Passed capacity ({capacity}) must be greater than existing capacity ({_capacity})!");
#endif

            var heap = CesMemoryUtility.AllocateCache<T>(capacity, _allocator);

            if (_capacity > 0)
            {
                CesMemoryUtility.CopyAndFree(_capacity, heap, _heap, _allocator);
            }

            _heap = heap;
            _capacity = capacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ResizeIfFull()
        {
            if (Hint.Unlikely(Count == _capacity))
            {
                SetCapacity(CesCollectionsUtility.CapacityUp(_capacity));
            }
        }
    }
}