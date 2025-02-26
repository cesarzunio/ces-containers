using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;

namespace Ces.Collections
{
    [NoAlias]
    public unsafe readonly struct RawSpan<T> where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction, NoAlias]
        readonly T* _data;

        public readonly int Length;

        public readonly bool IsCreated => _data != null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RawSpan(T* data, int length)
        {
            _data = data;
            Length = length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RawSpan<T> Null()
        {
            return new RawSpan<T>(null, 0);
        }

        public readonly ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if CES_COLLECTIONS_CHECK
                if (CesCollectionsUtility.IsOutOfRange(index, Length))
                    throw new Exception($"RawSpan :: this[] :: Index ({index}) out of range ({Length})!");
#endif

                return ref _data[index];
            }
        }

        public readonly ref T this[uint index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this[(int)index];
        }
    }
}