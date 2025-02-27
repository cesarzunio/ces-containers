using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Ces.Collections
{
    [NoAlias]
    public unsafe struct DatabaseTable<TInstance, TColumns> : IDatabaseTable
        where TInstance : unmanaged
        where TColumns : unmanaged, IDatabaseColumns<TInstance, TColumns>
    {
        const int CAPACITY_MIN = 256;

        public int Count;
        public int Capacity;
        readonly Allocator _allocator;

        public TColumns Columns;

        [NativeDisableUnsafePtrRestriction, NoAlias]
        public DatabaseId* IndexToId;

        public readonly bool IsCreated => IndexToId != null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DatabaseTable(Allocator allocator, int count)
        {
            if (count < 0)
                throw new Exception($"DatabaseTable :: Count ({count}) is negative!");

            int capacity = CesCollectionsUtility.CapacityInitialAligned(count, CAPACITY_MIN);

            Count = count;
            Capacity = capacity;
            _allocator = allocator;

            Columns = default;
            Columns.Allocate(_allocator, capacity);

            IndexToId = CesMemoryUtility.AllocateCacheDefault(capacity, _allocator, DatabaseId.Invalid);
        }

//        /// <summary>
//        /// Right after this constructor a manual copy into Columns must happen, <br></br>
//        /// as Count is initialized yet Columns are raw, uninitialized memory
//        /// </summary>
//        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
//        [MethodImpl(MethodImplOptions.NoInlining)]
//        public DatabaseTable(DatabaseTableSave save)
//        {
//#if CES_COLLECTIONS_CHECK
//            // not possible, leave it to catch serialization bugs
//            if (save.Count < 0)
//                throw new Exception($"DatabaseTable :: Count ({save.Count}) must be positive!");
//#endif

//            int capacityInitialAligned = CesCollectionsUtility.CapacityInitialAligned(CAPACITY_MIN, save.Count);

//            Count = save.Count;
//            Capacity = capacityInitialAligned;
//            _allocator = save.Allocator;

//            Columns = default;
//            Columns.Allocate(_allocator, capacityInitialAligned);

//            // its null if Count was 0 during serialization
//            if (save.IndexToId == null)
//            {
//                save.IndexToId = CesMemoryUtility.AllocateCache<DatabaseId>(capacityInitialAligned, _allocator);
//            }

//            IndexToId = save.IndexToId;
//        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (!IsCreated)
                throw new Exception($"DatabaseTable :: Dispose :: Is not created!");

            Columns.Dispose(_allocator);
            CesMemoryUtility.FreeAndNullify(ref IndexToId, _allocator);
        }

        public void IncreaseCapacity()
        {
            int capacity = CesCollectionsUtility.CapacityUp(Capacity);

#if CES_COLLECTIONS_CHECK
            if (!IsCreated)
                throw new Exception($"DatabaseTable:: IncreaseCapacity :: Is not created!");
#endif

            var columns = new TColumns();
            columns.Allocate(_allocator, capacity);

            var indexToId = CesMemoryUtility.AllocateCache<DatabaseId>(capacity, _allocator);

            columns.Copy(in Columns, Capacity);
            Columns.Dispose(_allocator);

            CesMemoryUtility.CopyAndFree(Capacity, indexToId, IndexToId, _allocator);

            for (int i = Count; i < capacity; i++)
            {
                indexToId[i] = DatabaseId.Invalid;
            }

            Capacity = capacity;
            Columns = columns;
            IndexToId = indexToId;
        }

        #region Serialization

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int GetCount()
        {
            return Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly DatabaseId* GetIndexToId()
        {
            return IndexToId;
        }

        #endregion
    }
}