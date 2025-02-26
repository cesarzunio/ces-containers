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
    public unsafe struct RawSet<T> : IDisposable
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
        public RawSet(Allocator allocator, int capacity, int alignment)
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
        public static RawSet<T> Null()
        {
            return new(Allocator.None, 0, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (!IsCreated)
            {
#if CES_COLLECTIONS_WARNING
                Debug.LogError("RawSet :: Dispose :: Is not created!");
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
                    throw new Exception($"RawSet :: this[] :: Index ({index}) out of range ({Count})!");
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
                throw new Exception($"RawSet :: Ptr :: Index ({index}) out of range ({Count})!");
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
                throw new Exception("RawSet :: Add :: Is not allocated!");
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
                throw new Exception($"RawSet :: Insert :: Index ({index}) out of range ({Count})!");
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
                throw new Exception($"RawSet :: RemoveAt :: Index ({index}) out of range ({Count})!");
#endif

            int indexLast = --Count;

            if (Hint.Likely(index != indexLast))
            {
                Data[index] = Data[indexLast];
            }
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
                throw new Exception($"RawSet :: RemoveRange :: Index ({index}) out of range ({Count})!");

            if (CesCollectionsUtility.IsOutOfRange(index + count - 1, Count))
                throw new Exception($"RawSet :: RemoveRange :: Index + count - 1 ({index + count - 1}) out of range ({Count})!");

            if (count < 0)
                throw new Exception($"RawSet :: RemoveRange :: Count ({count}) must be positive!");
#endif

            int remaining = Count - (index + count);

            if (remaining > 0)
            {
                CesMemoryUtility.Copy(remaining, Data + index, Data + index + count);
            }

            Count -= count;
        }

        void SetCapacity(int capacity)
        {
#if CES_COLLECTIONS_CHECK
            if (capacity <= _capacity)
                throw new Exception($"RawSet :: SetCapacity :: Passed capacity ({capacity}) must be greater than existing capacity ({_capacity})!");
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
    }
}