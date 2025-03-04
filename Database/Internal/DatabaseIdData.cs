using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Ces.Collections
{
    [NoAlias]
    public unsafe struct DatabaseIdData : IDatabaseIdData
    {
        const int CAPACITY_MIN = 128;

        public int StackCount;
        public int Capacity;
        readonly Allocator _allocator;

        [NativeDisableUnsafePtrRestriction, NoAlias]
        public DatabaseIndex* IdToIndex;

        [NativeDisableUnsafePtrRestriction, NoAlias]
        public uint* IdToUseCount;

        [NativeDisableUnsafePtrRestriction, NoAlias]
        public DatabaseId* IdStack;

        public readonly bool IsCreated => IdToIndex != null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DatabaseIdData(Allocator allocator, int capacity, bool initializeFully)
        {
            if (capacity < 0)
                throw new Exception($"DatabaseIdData :: Capacity ({capacity}) must be positive!");

            capacity = CesCollectionsUtility.CapacityInitialAligned(capacity, CAPACITY_MIN);

            StackCount = 0;
            Capacity = capacity;
            _allocator = allocator;

            IdToIndex = CesMemoryUtility.AllocateCacheDefault(capacity, _allocator, DatabaseIndex.Invalid);
            IdToUseCount = CesMemoryUtility.AllocateCacheDefault(capacity, _allocator, 0u);
            IdStack = CesMemoryUtility.AllocateCache<DatabaseId>(capacity, _allocator);

            if (initializeFully)
            {
                RecreateIdStack();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (!IsCreated)
                throw new Exception($"DatabaseIdData :: Dispose :: Is not created!");

            CesMemoryUtility.FreeAndNullify(ref IdToIndex, _allocator);
            CesMemoryUtility.FreeAndNullify(ref IdToUseCount, _allocator);
            CesMemoryUtility.FreeAndNullify(ref IdStack, _allocator);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void IncreaseCapacity()
        {
            int capacity = CesCollectionsUtility.CapacityUp(Capacity);

#if CES_COLLECTIONS_CHECK
            if (!IsCreated)
                throw new Exception($"DatabaseIdData :: IncreaseCapacity :: Is not created!");
#endif

            var idToIndex = CesMemoryUtility.AllocateCache<DatabaseIndex>(capacity, _allocator);
            var idToUseCount = CesMemoryUtility.AllocateCache<uint>(capacity, _allocator);
            var idStack = CesMemoryUtility.AllocateCache<DatabaseId>(capacity, _allocator);

            CesMemoryUtility.CopyAndFree(capacity, idToIndex, IdToIndex, _allocator);
            CesMemoryUtility.CopyAndFree(capacity, idToUseCount, IdToUseCount, _allocator);
            UnsafeUtility.Free(IdStack, _allocator);

            for (int i = Capacity; i < capacity; i++)
            {
                idToIndex[i] = DatabaseIndex.Invalid;
                idToUseCount[i] = 0;
            }

            Capacity = capacity;
            IdToIndex = idToIndex;
            IdToUseCount = idToUseCount;
            IdStack = idStack;

            FillIdStack(Capacity, capacity - 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RecreateIdStack()
        {
            FillIdStack(0, Capacity - 1);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        void FillIdStack(int fromInclusive, int toInclusive)
        {
#if CES_COLLECTIONS_CHECK
            if (StackCount != 0)
                throw new Exception($"DatabaseIdData :: FillIdStack :: StackCount ({StackCount}) must be 0 when filling!");
#endif

            for (int i = toInclusive; i >= fromInclusive; i--)
            {
                if (IdToUseCount[i] == 0)
                {
                    IdStack[StackCount++] = new DatabaseId(i);
                }
            }
        }

        #region Serialization

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int GetCapacity()
        {
            return Capacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly DatabaseIndex* GetIdToIndex()
        {
            return IdToIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly uint* GetIdToUseCount()
        {
            return IdToUseCount;
        }

        #endregion
    }
}