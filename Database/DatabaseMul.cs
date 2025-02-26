using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using static UnityEngine.Rendering.DebugUI;

namespace Ces.Collections
{
    [NoAlias]
    public unsafe struct DatabaseMul<TInstance, TColumns>
        where TInstance : unmanaged
        where TColumns : unmanaged, IDatabaseColumns<TInstance, TColumns>
    {
        public DatabaseMulIdData IdData;

        [NoAlias]
        public DatabaseTable<TInstance, TColumns>* Tables;
        public readonly int TablesAmount;

        readonly Allocator _allocator;

        public readonly bool IsCreated => Tables != null;

        /// <summary>
        /// Initial constructor
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DatabaseMul(Allocator allocator, int capacityId, int tablesAmount)
        {
            if (tablesAmount <= 0)
                throw new Exception($"DatabaseMul :: TablesAmount ({tablesAmount}) must be positive!");

            _allocator = allocator;

            IdData = new DatabaseMulIdData(allocator, capacityId, true);
            Tables = CesMemoryUtility.AllocateCache<DatabaseTable<TInstance, TColumns>>(tablesAmount, allocator);
            TablesAmount = tablesAmount;

            for (int i = 0; i < tablesAmount; i++)
            {
                Tables[i] = new DatabaseTable<TInstance, TColumns>(allocator, 0);
            }
        }

        /// <summary>
        /// Serialization constructor
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DatabaseMul(Allocator allocator, int capacityId, int tablesAmount, int* countsTables)
        {
            if (tablesAmount <= 0)
                throw new Exception($"DatabaseMul :: TablesAmount ({tablesAmount}) must be positive!");

            _allocator = allocator;

            IdData = new DatabaseMulIdData(allocator, capacityId, false);
            Tables = CesMemoryUtility.AllocateCache<DatabaseTable<TInstance, TColumns>>(tablesAmount, allocator);
            TablesAmount = tablesAmount;

            for (int i = 0; i < tablesAmount; i++)
            {
                Tables[i] = new DatabaseTable<TInstance, TColumns>(allocator, countsTables[i]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (!IsCreated)
                throw new Exception($"DatabaseMul :: Dispose :: Is not created!");

            IdData.Dispose();

            for (int i = 0; i < TablesAmount; i++)
            {
                Tables[i].Dispose();
            }

            CesMemoryUtility.FreeAndNullify(ref Tables, _allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly DatabaseMulIndex Add(TInstance instance, int tableIndex)
        {
            ref var table = ref MapTableIndexToTable(tableIndex);

            int index = GetNewIndex(ref table);
            table.Columns.Set(index, instance);

            return new DatabaseMulIndex(tableIndex, index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DatabaseIdIndexPair AddAndCreateId(TInstance instance, int tableIndex)
        {
            ref var table = ref MapTableIndexToTable(tableIndex);

            int index = GetNewIndex(ref table);
            table.Columns.Set(index, instance);

            var databaseIndex = new DatabaseMulIndex(tableIndex, index);

            return new DatabaseIdIndexPair(CreateIdFromIndex(databaseIndex), index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly TInstance Get(DatabaseMulIndex databaseIndex)
        {
            ref var table = ref MapTableIndexToTable(databaseIndex.TableIndex);

#if CES_COLLECTIONS_CHECK
            if (CesCollectionsUtility.IsOutOfRange(databaseIndex.Index, table.Count))
                throw new Exception($"Database :: Get :: Index ({databaseIndex.Index}) out of range ({table.Count})!");
#endif

            return table.Columns.Get(databaseIndex.Index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Set(DatabaseMulIndex databaseIndex, TInstance instance)
        {
            ref var table = ref MapTableIndexToTable(databaseIndex.TableIndex);

#if CES_COLLECTIONS_CHECK
            if (CesCollectionsUtility.IsOutOfRange(databaseIndex.Index, table.Count))
                throw new Exception($"Database :: Set :: Index ({databaseIndex.Index}) out of range ({table.Count})!");
#endif

            table.Columns.Set(databaseIndex.Index, instance);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DatabaseId CreateIdFromIndex(DatabaseMulIndex databaseIndex)
        {
            ref var id = ref MapIndexToId(databaseIndex);

            // if given index has no Id assigned, create new Id
            if (id.IsInvalid)
            {
                id = CreateId();
            }

            MapIdToIndex(id) = databaseIndex;
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

            ref var index = ref MapIdToIndex(id);

            // if object with this Id actually exists AND Id is about to be freed
            // then unlink the object's index and the Id
            if (!index.IsInvalid)
            {
                MapIndexToId(index) = DatabaseId.Invalid;
                index = DatabaseMulIndex.Invalid;
            }

#if CES_COLLECTIONS_CHECK
            if (IdData.StackCount == IdData.Capacity)
                throw new Exception($"Database :: ReleaseId :: Stack is full ({IdData.Capacity})!");
#endif

            IdData.IdStack[IdData.StackCount++] = id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Destroy(DatabaseMulIndex databaseIndex)
        {
            ref var table = ref MapTableIndexToTable(databaseIndex.TableIndex);

#if CES_COLLECTIONS_CHECK
            if (CesCollectionsUtility.IsOutOfRange(databaseIndex.Index, table.Count))
                throw new Exception($"Database :: Destroy :: Index ({databaseIndex.Index}) out of range ({table.Count})!");
#endif

            ref var id = ref MapIndexToId(ref table, databaseIndex.Index);

            // if object with has an Id assigned
            // then unlink the object's index and the Id
            if (!id.IsInvalid)
            {
                MapIdToIndex(id) = DatabaseMulIndex.Invalid;
                id = DatabaseId.Invalid;
            }

            int indexLast = --table.Count;
            table.Columns.Move(indexLast, databaseIndex.Index);
        }

        #region Mapping

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ref DatabaseMulIndex MapIdToIndex(DatabaseId id)
        {
#if CES_COLLECTIONS_CHECK
            if (CesCollectionsUtility.IsOutOfRange(id.Value, IdData.Capacity))
                throw new Exception($"Database :: MapIdToIndex :: Id ({id.Value}) out of range ({IdData.Capacity})!");
#endif

            return ref IdData.IdToIndex[id.Value];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryMapIdToIndex(DatabaseId id, out DatabaseMulIndex databaseIndex)
        {
            if (id.IsInvalid)
            {
                databaseIndex = DatabaseMulIndex.Invalid;
                return false;
            }

#if CES_COLLECTIONS_CHECK
            if (CesCollectionsUtility.IsOutOfRange(id.Value, IdData.Capacity))
                throw new Exception($"Database :: TryMapIdToIndex :: Id ({id.Value}) out of range ({IdData.Capacity})!");
#endif

            databaseIndex = MapIdToIndex(id);
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
        public readonly ref DatabaseId MapIndexToId(DatabaseMulIndex databaseIndex)
        {
            ref readonly var table = ref MapTableIndexToTable(databaseIndex.TableIndex);

#if CES_COLLECTIONS_CHECK
            if (CesCollectionsUtility.IsOutOfRange(databaseIndex.Index, table.Capacity))
                throw new Exception($"Database :: MapIndexToId :: Index ({databaseIndex.Index}) out of range ({table.Capacity})!");
#endif

            return ref table.IndexToId[databaseIndex.Index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly ref DatabaseId MapIndexToId(ref DatabaseTable<TInstance, TColumns> table, int index)
        {
#if CES_COLLECTIONS_CHECK
            if (CesCollectionsUtility.IsOutOfRange(index, table.Capacity))
                throw new Exception($"Database :: MapIndexToId :: Index ({index}) out of range ({table.Capacity})!");
#endif

            return ref table.IndexToId[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryMapIndexToId(DatabaseMulIndex databaseIndex, out DatabaseId id)
        {
            ref readonly var table = ref MapTableIndexToTable(databaseIndex.TableIndex);

#if CES_COLLECTIONS_CHECK
            if (CesCollectionsUtility.IsOutOfRange(databaseIndex.Index, table.Capacity))
                throw new Exception($"Database :: TryMapIndexToId :: Index ({databaseIndex.Index}) out of range ({table.Capacity})!");
#endif

            id = table.IndexToId[databaseIndex.Index];
            return !id.IsInvalid;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ref DatabaseTable<TInstance, TColumns> MapTableIndexToTable(int tableIndex)
        {
#if CES_COLLECTIONS_CHECK
            if (CesCollectionsUtility.IsOutOfRange(tableIndex, TablesAmount))
                throw new Exception($"Database :: MapTableIndexToTable :: TableIndex ({tableIndex}) out of range ({TablesAmount})!");
#endif

            return ref Tables[tableIndex];
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly int GetNewIndex(ref DatabaseTable<TInstance, TColumns> table)
        {
            if (Hint.Unlikely(table.Count == table.Capacity))
            {
                table.IncreaseCapacity();
            }

            return table.Count++;
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