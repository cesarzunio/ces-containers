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
    public unsafe struct RawList<T> : IDisposable
        where T : unmanaged
    {
        const int CAPACITY_MIN = 4;

        [NativeDisableUnsafePtrRestriction, NoAlias]
        public T* Data;

        public int Count;
        int _capacity;
        readonly int _alignment;
        readonly Allocator _allocator;

        public readonly bool IsCreated => Data != null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RawList(Allocator allocator, int capacity, int alignment)
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
        public static RawList<T> Null()
        {
            return new(Allocator.None, 0, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (!IsCreated)
            {
#if CES_COLLECTIONS_WARNING
                Debug.LogError("RawList :: Dispose :: Is not created!");
#endif

                return;
            }

            CesMemoryUtility.FreeAndNullify(ref Data, _allocator);
        }

        public readonly ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if CES_COLLECTIONS_CHECK
                if (CesCollectionsUtility.IsOutOfRange(index, Count))
                    throw new Exception($"RawList :: this[] :: Index ({index}) out of range ({Count})!");
#endif

                return ref Data[index];
            }
        }

        public readonly ref T this[uint index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this[(int)index];
        }

        public readonly ref T Last
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this[Count - 1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly T* Ptr(int index)
        {
#if CES_COLLECTIONS_CHECK
            if (CesCollectionsUtility.IsOutOfRange(index, Count))
                throw new Exception($"RawList :: Ptr :: Index ({index}) out of range ({Count})!");
#endif

            return Data + index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly T* Ptr(uint index)
        {
            return Ptr((int)index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            Count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Add(T value)
        {
#if CES_COLLECTIONS_CHECK
            if (_allocator == Allocator.None)
                throw new Exception("RawList :: Add :: Is not allocated!");
#endif

            ResizeIfFull();
            Data[Count++] = value;

            return Count - 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Insert(T value, int index)
        {
#if CES_COLLECTIONS_CHECK
            if (CesCollectionsUtility.IsOutOfRange(index, Count))
                throw new Exception($"RawList :: Insert :: Index ({index}) out of range ({Count})!");
#endif

            ResizeIfFull();
            int elementsToMove = Count - index;

            if (elementsToMove > 0)
            {
                CesMemoryUtility.ShiftRightByOne(Data + index + 1, elementsToMove);
            }

            Data[index] = value;
            Count++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAt(int index)
        {
#if CES_COLLECTIONS_CHECK
            if (CesCollectionsUtility.IsOutOfRange(index, Count))
                throw new Exception($"RawList :: RemoveAt :: Index ({index}) out of range ({Count})!");
#endif

            int elementsToMove = Count - index - 1;

            if (elementsToMove > 0)
            {
                CesMemoryUtility.ShiftLeftByOne(Data + index, elementsToMove);
            }

            Count--;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reverse()
        {
            for (int i = 0, j = Count - 1; i < j; i++, j--)
            {
                (Data[j], Data[i]) = (Data[i], Data[j]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveRange(int index, int count)
        {
#if CES_COLLECTIONS_CHECK
            if (CesCollectionsUtility.IsOutOfRange(index, Count))
                throw new Exception($"RawList :: RemoveRange :: Index ({index}) out of range ({Count})!");

            if (CesCollectionsUtility.IsOutOfRange(index + count - 1, Count))
                throw new Exception($"RawList :: RemoveRange :: Index + count - 1 ({index + count - 1}) out of range ({Count})!");

            if (count < 0)
                throw new Exception($"RawList :: RemoveRange :: Count ({count}) must be positive!");
#endif

            int remaining = Count - (index + count);

            if (remaining > 0)
            {
                CesMemoryUtility.Copy(remaining, Data + index, Data + index + count);
            }

            Count -= count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReserveAtStart(int elementsToReserve)
        {
#if CES_COLLECTIONS_CHECK
            if (elementsToReserve <= 0)
                throw new Exception($"RawList :: ReserveAtStart :: ElementsToReserve ({elementsToReserve}) must be positive!");
#endif

            EnsureCapacity(Count + elementsToReserve);

            long size = Count * UnsafeUtility.SizeOf<T>();
            UnsafeUtility.MemMove(Data + elementsToReserve, Data, size);
        }

        void SetCapacity(int capacity)
        {
#if CES_COLLECTIONS_CHECK
            if (capacity <= _capacity)
                throw new Exception($"RawList :: SetCapacity :: Passed capacity ({capacity}) must be greater than existing capacity ({_capacity})!");
#endif

            var data = CesMemoryUtility.Allocate<T>(capacity, _alignment, _allocator);

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
        void EnsureCapacity(int capacity)
        {
            if (Hint.Unlikely(capacity > _capacity))
            {
                SetCapacity(CesCollectionsUtility.CapacityInitialAligned(CAPACITY_MIN, capacity));
            }
        }
    }
}