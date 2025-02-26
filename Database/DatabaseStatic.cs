using System;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace Ces.Collections
{
    public unsafe struct DatabaseStatic<TInstance, TColumns>
        where TInstance : unmanaged
        where TColumns : unmanaged, IDatabaseColumns<TInstance, TColumns>
    {
        public DatabaseTableStatic<TInstance, TColumns> Table;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DatabaseStatic(Allocator allocator, int length)
        {
            Table = new DatabaseTableStatic<TInstance, TColumns>(allocator, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            Table.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly TInstance Get(int index)
        {
#if CES_COLLECTIONS_CHECK
            if (CesCollectionsUtility.IsOutOfRange(index, Table.Length))
                throw new Exception($"DatabaseStatic :: Get :: Index ({index}) out of range ({Table.Length})!");
#endif

            return Table.Columns.Get(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Set(int index, TInstance instance)
        {
#if CES_COLLECTIONS_CHECK
            if (CesCollectionsUtility.IsOutOfRange(index, Table.Length))
                throw new Exception($"DatabaseStatic :: Set :: Index ({index}) out of range ({Table.Length})!");
#endif

            Table.Columns.Set(index, instance);
        }
    }
}