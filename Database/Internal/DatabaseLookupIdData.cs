using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Ces.Collections
{
    [NoAlias]
    public unsafe struct DatabaseLookupIdData<TLookup> : IDatabaseIdData
        where TLookup : unmanaged
    {
        const int CAPACITY_MIN = 128;

        public int StackCount;
        public int Capacity;
        readonly Allocator _allocator;
        public readonly TLookup LookupValueDefault;

        [NativeDisableUnsafePtrRestriction, NoAlias]
        public DatabaseIndex* IdToIndex;

        [NativeDisableUnsafePtrRestriction, NoAlias]
        public uint* IdToUseCount;

        [NativeDisableUnsafePtrRestriction, NoAlias]
        public DatabaseId* IdStack;

        [NativeDisableUnsafePtrRestriction, NoAlias]
        public TLookup* LookupMatrix;

        public readonly bool IsCreated => IdToIndex != null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DatabaseLookupIdData(Allocator allocator, int capacity, bool initializeFully, TLookup lookupValueDefault)
        {
            if (capacity < 0)
                throw new Exception($"DatabaseLookupIdData :: Capacity ({capacity}) must be positive!");

            int capacityInitialAligned = CesCollectionsUtility.CapacityInitialAligned(CAPACITY_MIN, capacity);

            StackCount = 0;
            Capacity = capacityInitialAligned;
            _allocator = allocator;
            LookupValueDefault = lookupValueDefault;

            IdToIndex = CesMemoryUtility.AllocateCacheDefault(capacityInitialAligned, _allocator, DatabaseIndex.Invalid);
            IdToUseCount = CesMemoryUtility.AllocateCacheDefault(capacityInitialAligned, _allocator, 0u);
            IdStack = CesMemoryUtility.AllocateCache<DatabaseId>(capacityInitialAligned, _allocator);

            int lookupMatrixLength = DatabaseLookupMatrixUtility.GetLookupMatrixLength(capacityInitialAligned);
            LookupMatrix = CesMemoryUtility.AllocateCacheDefault(lookupMatrixLength, _allocator, lookupValueDefault);

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
            CesMemoryUtility.FreeAndNullify(ref LookupMatrix, _allocator);
        }

        public void IncreaseCapacity()
        {
            int capacity = CesCollectionsUtility.CapacityUp(Capacity);

#if CES_COLLECTIONS_CHECK
            if (!IsCreated)
                throw new Exception($"DatabaseIdData :: IncreaseCapacity :: Is not created!");
#endif

            int matrixLengthOld = DatabaseLookupMatrixUtility.GetLookupMatrixLength(Capacity);
            int matrixLength = DatabaseLookupMatrixUtility.GetLookupMatrixLength(capacity);

            var idToIndex = CesMemoryUtility.AllocateCache<DatabaseIndex>(capacity, _allocator);
            var idToUseCount = CesMemoryUtility.AllocateCache<uint>(capacity, _allocator);
            var idStack = CesMemoryUtility.AllocateCache<DatabaseId>(capacity, _allocator);
            var lookupMatrix = CesMemoryUtility.AllocateCache<TLookup>(matrixLength, _allocator);

            CesMemoryUtility.CopyAndFree(capacity, idToIndex, IdToIndex, _allocator);
            CesMemoryUtility.CopyAndFree(capacity, idToUseCount, IdToUseCount, _allocator);
            UnsafeUtility.Free(IdStack, _allocator);
            CesMemoryUtility.CopyAndFree(matrixLengthOld, lookupMatrix, LookupMatrix, _allocator);

            for (int i = Capacity; i < capacity; i++)
            {
                idToIndex[i] = DatabaseIndex.Invalid;
                idToUseCount[i] = 0;
            }

            for (int i = matrixLengthOld; i < matrixLength; i++)
            {
                lookupMatrix[i] = LookupValueDefault;
            }

            Capacity = capacity;
            IdToIndex = idToIndex;
            IdToUseCount = idToUseCount;
            IdStack = idStack;
            LookupMatrix = lookupMatrix;

            FillIdStack(Capacity, capacity - 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RecreateIdStack()
        {
            FillIdStack(0, Capacity - 1);
        }

        void FillIdStack(int fromInclusive, int toInclusive)
        {
#if CES_COLLECTIONS_CHECK
            if (StackCount != 0)
                throw new Exception($"DatabaseLookupIdData :: FillIdStack :: StackCount ({StackCount}) must be 0 when filling!");
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