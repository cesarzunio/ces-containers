using System;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Burst;

namespace Ces.Collections
{
    [NoAlias]
    public unsafe struct RawArray<T> : IDisposable
        where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction, NoAlias]
        public T* Data;

        public readonly int Length;
        readonly Allocator _allocator;

        public readonly bool IsCreated => Data != null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RawArray(Allocator allocator, int length, int alignment)
        {
            if (allocator == Allocator.None || length <= 0)
            {
                Data = null;
                Length = 0;
                _allocator = Allocator.None;
                return;
            }

            Data = CesMemoryUtility.Allocate<T>(length, alignment, allocator);
            Length = length;
            _allocator = allocator;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RawArray(Allocator allocator, int length, int alignment, RawParams<T> rawParams) : this(allocator, length, alignment)
        {
            if ((rawParams.Flag & RawParamsFlag.ClearMemory) == RawParamsFlag.ClearMemory)
            {
                CesMemoryUtility.MemSet(Data, default, length);
            }
            else if ((rawParams.Flag & RawParamsFlag.SetValueDefault) == RawParamsFlag.SetValueDefault)
            {
                SetAllElements(rawParams.ValueDefault);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RawArray<T> Null()
        {
            return new(Allocator.None, 0, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (!IsCreated)
            {
#if CES_COLLECTIONS_WARNING
                Debug.LogError("RawArray :: Dispose :: Is not created!");
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
                if (CesCollectionsUtility.IsOutOfRange(index, Length))
                    throw new Exception($"RawArray :: this[] :: Index ({index}) out of range ({Length})!");
#endif

                return ref Data[index];
            }
        }

        public readonly ref T this[uint index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref this[(int)index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void SetAllElements(T value)
        {
            CesMemoryUtility.MemSet(Data, value, Length);
        }
    }
}