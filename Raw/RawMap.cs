using System;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Burst.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;

namespace Ces.Collections
{
    [NoAlias]
    public unsafe struct RawMap<TKey, TValue> : IDisposable
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        const int CAPACITY_MIN = 4;
        const int NOT_PRESENT = -1;

        [NativeDisableUnsafePtrRestriction, NoAlias]
        public Element* Data;

        public int Count;
        int _capacity;
        readonly int _alignment;
        readonly Allocator _allocator;

        public readonly bool IsCreated => Data != null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RawMap(Allocator allocator, int capacity, int alignment)
        {
            Data = null;
            Count = 0;
            _capacity = 0;
            _alignment = alignment;
            _allocator = allocator;

            if (allocator == Allocator.None)
                return;

            capacity = math.max(capacity, CAPACITY_MIN);
            capacity = CesCollectionsUtility.CapacityInitialAligned(CAPACITY_MIN, capacity);

            SetCapacity(capacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RawMap<TKey, TValue> Null()
        {
            return new(Allocator.None, 0, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (!IsCreated)
            {
#if CES_COLLECTIONS_WARNING
                Debug.LogError("RawMap :: Dispose :: Is not created!");
#endif

                return;
            }

            CesMemoryUtility.FreeAndNullify(ref Data, _allocator);
        }

        public readonly ref TValue this[TKey key]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                int index = IndexOfKey(key);

#if CES_COLLECTIONS_CHECK
                if (index == NOT_PRESENT)
                    throw new Exception($"RawMap :: this[] :: Key ({key}) is not present!");
#endif

                return ref Data[index].Value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            Count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Contains(TKey key)
        {
            return IndexOfKey(key) >= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Add(TKey key, TValue value)
        {
#if CES_COLLECTIONS_CHECK
            if (_allocator == Allocator.None)
                throw new Exception("RawMap :: Add :: Is not allocated!");
#endif

            if (Contains(key))
                return false;

            ResizeIfFull();

            Data[Count++] = new Element
            {
                Key = key,
                Value = value,
            };

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddOrSet(TKey key, TValue value)
        {
#if CES_COLLECTIONS_CHECK
            if (_allocator == Allocator.None)
                throw new Exception("RawMap :: Add :: Is not allocated!");
#endif

            int index = IndexOfKey(key);

            if (index != NOT_PRESENT)
            {
                Data[index].Value = value;
                return;
            }

            ResizeIfFull();

            Data[Count++] = new Element
            {
                Key = key,
                Value = value,
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(TKey key)
        {
            // is this check needed ? if not allocated, count == 0, so will fail anyway. maybe just warning ?
#if CES_COLLECTIONS_CHECK
            if (_allocator == Allocator.None)
                throw new Exception("RawMap :: Add :: Is not allocated!");
#endif

            int index = IndexOfKey(key);

            if (index == NOT_PRESENT)
                return false;

            int indexLast = --Count;

            if (Hint.Likely(index != indexLast))
            {
                Data[index] = Data[indexLast];
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(TKey key, out TValue* valuePtr)
        {
            // is this check needed ? if not allocated, count == 0, so will fail anyway. maybe just warning ?
#if CES_COLLECTIONS_CHECK
            if (_allocator == Allocator.None)
                throw new Exception("RawMap :: Add :: Is not allocated!");
#endif

            valuePtr = null;

            int index = IndexOfKey(key);

            if (index == NOT_PRESENT)
                return false;

            valuePtr = &Data[index].Value;
            return true;
        }

        void SetCapacity(int capacity)
        {
#if CES_COLLECTIONS_CHECK
            if (capacity <= _capacity)
                throw new Exception($"RawMap :: SetCapacity :: Passed capacity ({capacity}) must be greater than existing capacity ({_capacity})!");
#endif

            var data = CesMemoryUtility.Allocate<Element>(capacity, _alignment, _allocator);

            if (_capacity > 0)
            {
                CesMemoryUtility.CopyAndFree(_capacity, data, Data, _allocator);
            }

            Data = data;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly int IndexOfKey(TKey key)
        {
            for (int i = 0; i < Count; i++)
            {
                if (Data[i].Key.Equals(key))
                    return i;
            }

            return NOT_PRESENT;
        }

        public struct Element
        {
            public TKey Key;
            public TValue Value;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Element(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }
        }
    }
}