using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Ces.Collections
{
    [NoAlias]
    public unsafe struct RawPtr<T> where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction, NoAlias]
        public T* Ptr;

        readonly Allocator _allocator;

        public readonly bool IsCreated => Ptr != null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RawPtr(Allocator allocator, int alignment)
        {
            Ptr = CesMemoryUtility.Allocate<T>(1, alignment, allocator);
            _allocator = allocator;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RawPtr(Allocator allocator, int alignment, T value) : this(allocator, alignment)
        {
            *Ptr = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (!IsCreated)
                throw new Exception("RawPtr :: Dispose :: Is already disposed!");

            UnsafeUtility.Free(Ptr, _allocator);
            Ptr = null;
        }

        public readonly ref T Ref
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if CES_COLLECTIONS_CHECK
                if (!IsCreated)
                    throw new Exception("RawPtr :: Ref :: Ptr is null!");
#endif

                return ref (*Ptr);
            }
        }
    }
}