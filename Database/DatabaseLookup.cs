using System;
using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.VisualScripting;

namespace Ces.Collections
{
    public unsafe struct DatabaseLookup<TInstance, TColumns, TLookup>
        where TInstance : unmanaged
        where TColumns : unmanaged, IDatabaseColumns<TInstance, TColumns>
        where TLookup : unmanaged
    {
        public DatabaseLookupIdData<TLookup> IdData;
        public DatabaseTable<TInstance, TColumns> Table;

        /// <summary>
        /// Initial constructor
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DatabaseLookup(Allocator allocator, int capacityId, TLookup lookupValueDefault)
        {
            IdData = new DatabaseLookupIdData<TLookup>(allocator, capacityId, true, lookupValueDefault);
            Table = new DatabaseTable<TInstance, TColumns>(allocator, 0);
        }

        /// <summary>
        /// Serialization constructor
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DatabaseLookup(Allocator allocator, int capacityId, int countTable, TLookup lookupValueDefault)
        {
            IdData = new DatabaseLookupIdData<TLookup>(allocator, capacityId, false, lookupValueDefault);
            Table = new DatabaseTable<TInstance, TColumns>(allocator, countTable);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            IdData.Dispose();
            Table.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Add(TInstance instance)
        {
            int index = GetNewIndex();
            Table.Columns.Set(index, instance);

            return index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DatabaseIdIndexPair AddAndCreateId(TInstance instance)
        {
            int index = GetNewIndex();
            Table.Columns.Set(index, instance);

            return new DatabaseIdIndexPair(CreateIdFromIndex(index), index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly TInstance Get(int index)
        {
#if CES_COLLECTIONS_CHECK
            if (CesCollectionsUtility.IsOutOfRange(index, Table.Count))
                throw new Exception($"Database :: Get :: Index ({index}) out of range ({Table.Count})!");
#endif

            return Table.Columns.Get(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Set(int index, TInstance instance)
        {
#if CES_COLLECTIONS_CHECK
            if (CesCollectionsUtility.IsOutOfRange(index, Table.Count))
                throw new Exception($"Database :: Set :: Index ({index}) out of range ({Table.Count})!");
#endif

            Table.Columns.Set(index, instance);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DatabaseId CreateIdFromIndex(int index)
        {
            ref var id = ref MapIndexToId(index);

            // if given index has no Id assigned, create new Id
            if (id.IsInvalid)
            {
                id = CreateId();
            }

            MapIdToIndex(id) = new DatabaseIndex(index);
            MapIdToUseCount(id)++;

            return id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyId(DatabaseId id)
        {
            ref uint useCount = ref MapIdToUseCount(id);

#if CES_COLLECTIONS_CHECK
            if (useCount == 0)
                throw new Exception($"Database :: CopyId :: Id ({id.Value}) is not used!");
#endif

            MapIdToUseCount(id)++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleaseId(DatabaseId id)
        {
            ref uint useCount = ref MapIdToUseCount(id);

#if CES_COLLECTIONS_CHECK
            if (useCount == 0)
                throw new Exception($"Database :: ReleaseId :: Id ({id.Value}) is not used!");
#endif

            if (--useCount > 0)
                return;

            // Id is about to be freed, set all its lookups to default
            DatabaseLookupMatrixUtility.SetAllLookupValues(id.Value, IdData.LookupMatrix, IdData.Capacity, IdData.LookupValueDefault);

            ref var index = ref MapIdToIndex(id);

            // if object with this Id actually exists AND Id is about to be freed
            // then unlink the object's index and the Id
            if (!index.IsInvalid)
            {
                MapIndexToId(index.Index) = DatabaseId.Invalid;
                index = DatabaseIndex.Invalid;
            }

#if CES_COLLECTIONS_CHECK
            if (IdData.StackCount == IdData.Capacity)
                throw new Exception($"Database :: ReleaseId :: Stack is full ({IdData.Capacity})!");
#endif

            IdData.IdStack[IdData.StackCount++] = id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Destroy(int index)
        {
#if CES_COLLECTIONS_CHECK
            if (CesCollectionsUtility.IsOutOfRange(index, Table.Count))
                throw new Exception($"Database :: Destroy :: Index ({index}) out of range ({Table.Count})!");
#endif

            ref var id = ref MapIndexToId(index);

            // if object with has an Id assigned
            // then unlink the object's index and the Id
            if (!id.IsInvalid)
            {
                MapIdToIndex(id) = DatabaseIndex.Invalid;
                id = DatabaseId.Invalid;
            }

            int indexLast = --Table.Count;
            Table.Columns.Move(indexLast, index);
        }

        #region Mapping

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ref DatabaseIndex MapIdToIndex(DatabaseId id)
        {
#if CES_COLLECTIONS_CHECK
            if (CesCollectionsUtility.IsOutOfRange(id.Value, IdData.Capacity))
                throw new Exception($"Database :: MapIdToIndex :: Id ({id.Value}) out of range ({IdData.Capacity})!");
#endif

            return ref IdData.IdToIndex[id.Value];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryMapIdToIndex(DatabaseId id, out int index)
        {
            if (id.IsInvalid)
            {
                index = DatabaseIndex.INVALID;
                return false;
            }

#if CES_COLLECTIONS_CHECK
            if (CesCollectionsUtility.IsOutOfRange(id.Value, IdData.Capacity))
                throw new Exception($"Database :: TryMapIdToIndex :: Id ({id.Value}) out of range ({IdData.Capacity})!");
#endif

            ref readonly var databaseIndex = ref MapIdToIndex(id);

            index = databaseIndex.Index;
            return !databaseIndex.IsInvalid;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ref uint MapIdToUseCount(DatabaseId id)
        {
#if CES_COLLECTIONS_CHECK
            if (CesCollectionsUtility.IsOutOfRange(id.Value, IdData.Capacity))
                throw new Exception($"Database :: MapIdToUseCount :: Id ({id.Value}) out of range ({IdData.Capacity})!");
#endif

            return ref IdData.IdToUseCount[id.Value];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ref DatabaseId MapIndexToId(int index)
        {
#if CES_COLLECTIONS_CHECK
            if (CesCollectionsUtility.IsOutOfRange(index, Table.Capacity))
                throw new Exception($"Database :: MapIndexToId :: Index ({index}) out of range ({Table.Capacity})!");
#endif

            return ref Table.IndexToId[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryMapIndexToId(int index, out DatabaseId id)
        {
#if CES_COLLECTIONS_CHECK
            if (CesCollectionsUtility.IsOutOfRange(index, Table.Capacity))
                throw new Exception($"Database :: TryMapIndexToId :: Index ({index}) out of range ({Table.Capacity})!");
#endif

            id = Table.IndexToId[index];
            return !id.IsInvalid;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ref TLookup MapIdsToLookupValue(DatabaseId idA, DatabaseId idB)
        {
#if CES_COLLECTIONS_CHECK
            if (CesCollectionsUtility.IsOutOfRange(idA.Value, IdData.Capacity))
                throw new Exception($"DatabaseLookup :: MapIdsToLookupValue :: IdA ({idA}) out of range ({IdData.Capacity})!");

            if (CesCollectionsUtility.IsOutOfRange(idB.Value, IdData.Capacity))
                throw new Exception($"DatabaseLookup :: MapIdsToLookupValue :: IdB ({idB}) out of range ({IdData.Capacity})!");
#endif

            return ref IdData.LookupMatrix[DatabaseLookupMatrixUtility.GetLookupMatrixIndex(idA.Value, idB.Value)];
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int GetNewIndex()
        {
            if (Hint.Unlikely(Table.Count == Table.Capacity))
            {
                Table.IncreaseCapacity();
            }

            return Table.Count++;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        DatabaseId CreateId()
        {
            if (Hint.Unlikely(IdData.StackCount == 0))
            {
                IdData.IncreaseCapacity();
            }

            return IdData.IdStack[--IdData.StackCount];
        }
    }
}