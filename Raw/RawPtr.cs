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
            if (allocator == Allocator.None)
            {
                Ptr = null;
                _allocator = Allocator.None;
                return;
            }

            Ptr = CesMemoryUtility.Allocate<T>(1, alignment, allocator);
            _allocator = allocator;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RawPtr(Allocator allocator, int alignment, T value)
        {
            if (allocator == Allocator.None)
            {
                Ptr = null;
                _allocator = Allocator.None;
                return;
            }

            Ptr = CesMemoryUtility.Allocate<T>(1, alignment, allocator);
            *Ptr = value;
            _allocator = allocator;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RawPtr<T> Null()
        {
            return new(Allocator.None, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (!IsCreated)
            {
#if CES_COLLECTIONS_WARNING
                Debug.LogError("RawPtr :: Dispose :: Is not created!");
#endif
                return;
            }

            CesMemoryUtility.FreeAndNullify(ref Ptr, _allocator);
        }

        public readonly ref T Ref
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if CES_COLLECTIONS_CHECK
                if (!IsCreated)
                    throw new Exception("RawPtr :: Ref :: Is not created!");
#endif

                return ref *Ptr;
            }
        }
    }
}