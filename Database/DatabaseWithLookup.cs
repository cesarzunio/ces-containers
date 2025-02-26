//using System;
//using System.Runtime.CompilerServices;
//using Unity.Burst.CompilerServices;
//using Unity.Collections;
//using Unity.Mathematics;
//using UnityEngine.UIElements;

//namespace Ces.Collections
//{
//    public unsafe struct DatabaseWithLookup<TInstance, TColumns, TLookup> : IDatabase, IDatabaseWithLookup<TLookup>
//        where TInstance : unmanaged
//        where TColumns : unmanaged, IDatabaseColumns<TInstance, TColumns>
//        where TLookup : unmanaged
//    {
//        const int CAPACITY_MIN = 256;

//        public DatabaseIdDataWithLookup<TLookup> IdData;
//        public DatabaseTable2<TInstance, TColumns> Table;

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public DatabaseWithLookup(Allocator allocator, int capacityId, int capacityTable, TLookup lookupValueDefault, bool initializeFully)
//        {
//            IdData = new DatabaseIdDataWithLookup<TLookup>(allocator, capacityId, lookupValueDefault, initializeFully);
//            Table = new DatabaseTable2<TInstance, TColumns>(allocator, capacityTable);
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public void Dispose()
//        {
//            IdData.Dispose();
//            Table.Dispose();
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public int Add(TInstance instance)
//        {
//            int index = GetNewIndex();
//            Table.Columns.Set(index, instance);

//            return index;
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public DatabaseIdIndexPair AddAndCreateId(TInstance instance)
//        {
//            int index = GetNewIndex();
//            Table.Columns.Set(index, instance);

//            return new DatabaseIdIndexPair(CreateIdFromIndex(index), index);
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public readonly TInstance Get(int index)
//        {
//#if CES_COLLECTIONS_CHECK
//            if (CesCollectionsUtility.IsOutOfRange(index, Table.Count))
//                throw new Exception($"DatabaseWithLookup :: Get :: Index ({index}) out of range ({Table.Count})!");
//#endif

//            return Table.Columns.Get(index);
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public readonly void Set(int index, TInstance instance)
//        {
//#if CES_COLLECTIONS_CHECK
//            if (CesCollectionsUtility.IsOutOfRange(index, Table.Count))
//                throw new Exception($"DatabaseWithLookup :: Set :: Index ({index}) out of range ({Table.Count})!");
//#endif

//            Table.Columns.Set(index, instance);
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public DatabaseId CreateIdFromIndex(int index)
//        {
//            ref var id = ref MapIndexToId(index);

//            if (id.IsInvalid) // index has no id assigned, create new
//            {
//                id = CreateId();
//            }

//            MapIdToIndex(id) = new DatabaseIndex(index);
//            MapIdToUseCount(id)++;

//            return id;
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public void CopyId(DatabaseId id)
//        {
//            ref uint useCount = ref MapIdToUseCount(id);

//#if CES_COLLECTIONS_CHECK
//            if (useCount == 0)
//                throw new Exception($"DatabaseWithLookup :: CopyId :: Id ({id.Value}) is not used!");
//#endif

//            MapIdToUseCount(id)++;
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public void ReleaseId(DatabaseId id)
//        {
//            ref uint useCount = ref MapIdToUseCount(id);

//#if CES_COLLECTIONS_CHECK
//            if (useCount == 0)
//                throw new Exception($"DatabaseWithLookup :: ReleaseId :: Id ({id.Value}) is not used!");
//#endif

//            useCount--;

//            if (useCount == 0)
//            {
//                ref var index = ref MapIdToIndex(id);

//                if (!index.IsInvalid) // referenced object actually exists, destroy its id
//                {
//                    MapIndexToId(index.Index) = DatabaseId.Invalid;
//                    index = DatabaseIndex.Invalid;
//                }

//#if CES_COLLECTIONS_CHECK
//                if (IdData.StackCount == IdData.Capacity)
//                    throw new Exception($"DatabaseWithLookup :: ReleaseId :: Stack is full ({IdData.Capacity})!");
//#endif

//                IdData.IdStack[IdData.StackCount++] = id;
//            }
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public void Destroy(int index)
//        {
//#if CES_COLLECTIONS_CHECK
//            if (CesCollectionsUtility.IsOutOfRange(index, Table.Count))
//                throw new Exception($"DatabaseWithLookup :: Destroy :: Index ({index}) out of range ({Table.Count})!");
//#endif

//            ref var id = ref MapIndexToId(index);

//            if (!id.IsInvalid) // this index has id assigned, set the id to nothing without destroying it
//            {
//                MapIdToIndex(id) = DatabaseIndex.Invalid;
//                id = DatabaseId.Invalid;
//            }

//            int indexLast = --Table.Count;
//            Table.Columns.Move(indexLast, index);
//        }

//        #region Mapping

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public ref DatabaseIndex MapIdToIndex(DatabaseId id)
//        {
//#if CES_COLLECTIONS_CHECK
//            if (CesCollectionsUtility.IsOutOfRange(id.Value, IdData.Capacity))
//                throw new Exception($"DatabaseWithLookup :: MapIdToIndex :: Id ({id.Value}) out of range ({IdData.Capacity})!");
//#endif

//            return ref IdData.IdToIndex[id.Value];
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public readonly bool TryMapIdToIndex(DatabaseId id, out int index)
//        {
//            if (id.IsInvalid)
//            {
//                index = default;
//                return false;
//            }

//#if CES_COLLECTIONS_CHECK
//            if (CesCollectionsUtility.IsOutOfRange(id.Value, IdData.Capacity))
//                throw new Exception($"DatabaseWithLookup :: TryMapIdToIndex :: Id ({id.Value}) out of range ({IdData.Capacity})!");
//#endif

//            ref readonly var databaseIndex = ref IdData.IdToIndex[id.Value];

//            index = databaseIndex.Index;
//            return !databaseIndex.IsInvalid;
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public ref uint MapIdToUseCount(DatabaseId id)
//        {
//#if CES_COLLECTIONS_CHECK
//            if (CesCollectionsUtility.IsOutOfRange(id.Value, IdData.Capacity))
//                throw new Exception($"DatabaseWithLookup :: MapIdToUseCount :: Id ({id.Value}) out of range ({IdData.Capacity})!");
//#endif

//            return ref IdData.IdToUseCount[id.Value];
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public ref DatabaseId MapIndexToId(int index)
//        {
//#if CES_COLLECTIONS_CHECK
//            if (CesCollectionsUtility.IsOutOfRange(index, IdData.Capacity))
//                throw new Exception($"DatabaseWithLookup :: MapIndexToId :: Index ({index}) out of range ({Table.Capacity})!");
//#endif

//            return ref Table.IndexToId[index];
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public readonly bool TryMapIndexToId(int index, out DatabaseId id)
//        {
//#if CES_COLLECTIONS_CHECK
//            if (CesCollectionsUtility.IsOutOfRange(index, IdData.Capacity))
//                throw new Exception($"DatabaseWithLookup :: TryMapIndexToId :: Index ({index}) out of range ({Table.Capacity})!");
//#endif

//            id = Table.IndexToId[index];
//            return !id.IsInvalid;
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public ref TLookup MapIdsToLookupValue(DatabaseId idA, DatabaseId idB)
//        {
//#if CES_COLLECTIONS_CHECK
//            if (CesCollectionsUtility.IsOutOfRange(idA.Value, IdData.Capacity))
//                throw new Exception($"DatabaseWithLookup :: MapIdsToLookupValue :: Id A ({idA.Value}) out of range ({IdData.Capacity})!");

//            if (CesCollectionsUtility.IsOutOfRange(idB.Value, IdData.Capacity))
//                throw new Exception($"DatabaseWithLookup :: MapIdsToLookupValue :: Id B ({idB.Value}) out of range ({IdData.Capacity})!");
//#endif

//            return ref IdData.LookupMatrix[LookupMatrixUtility.LookupMatrixIndex(idA.Value, idB.Value)];
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public readonly void ClearIdChanges()
//        {
//            CesMemoryUtility.MemSet(IdData.IdToChange, DatabaseIdChange.Null, IdData.Capacity);
//        }

//        #endregion

//        #region Serialization

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public readonly int GetIdDataLength() => IdData.Capacity;

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public readonly int GetTableCount() => Table.Count;

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public void SetTableCount(int count) => Table.Count = count;

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public readonly int GetLookupMatrixLength() => LookupMatrixUtility.LookupMatrixLength(IdData.Capacity);

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public readonly DatabaseIndex* GetIdToIndex() => IdData.IdToIndex;

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public readonly uint* GetIdToUseCount() => IdData.IdToUseCount;

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public readonly DatabaseId* GetIndexToId() => Table.IndexToId;

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public readonly TLookup* GetLookupMatrix() => IdData.LookupMatrix;

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        public void RecreateIdStack() => IdData.RecreateIdStack();

//        #endregion

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        int GetNewIndex()
//        {
//            if (Hint.Unlikely(Table.Count == Table.Capacity))
//            {
//                //Table.SetCapacity(CesCollectionsUtility.CapacityUp(Table.Capacity));
//            }

//            return Table.Count++;
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        DatabaseId CreateId()
//        {
//            if (Hint.Unlikely(IdData.StackCount == 0))
//            {
//                //IdData.SetCapacity(CesCollectionsUtility.CapacityUp(IdData.Capacity));
//            }

//            return IdData.IdStack[--IdData.StackCount];
//        }
//    }
//}