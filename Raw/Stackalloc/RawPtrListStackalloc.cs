using System;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace Ces.Collections
{
    public unsafe struct RawPtrListStackalloc<T> where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        readonly T** _data;

        int _count;
        readonly int _capacity;

        public readonly int Count => _count;
        public readonly int Capacity => _capacity;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RawPtrListStackalloc(IntPtr* data, int capacity)
        {
            _data = (T**)data;
            _count = 0;
            _capacity = capacity;
        }

        public readonly T* this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if CES_COLLECTIONS_CHECK
                if (CesCollectionsUtility.IsOutOfRange(index, _count))
                    throw new Exception("RawPtrListStackalloc :: this[] :: Index out of range!");
#endif

                return _data[index];
            }
        }

        public readonly T* this[uint index] => this[(int)index];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T* value)
        {
            if (_count == _capacity)
                throw new Exception("RawListStackalloc :: List is full!");

            _data[_count++] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() => _count = 0;
    }
}