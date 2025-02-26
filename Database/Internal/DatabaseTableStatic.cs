using System;
using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Ces.Collections
{
    public unsafe struct DatabaseTableStatic<TInstance, TColumns>
        where TInstance : unmanaged
        where TColumns : unmanaged, IDatabaseColumns<TInstance, TColumns>
    {
        public readonly int Length;
        readonly Allocator _allocator;

        public TColumns Columns;

        public readonly bool IsCreated => Columns.IsCreated();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DatabaseTableStatic(Allocator allocator, int length)
        {
            if (length <= 0)
                throw new Exception($"DatabaseTableStatic :: Length ({length}) must be greater than 0!");

            Length = length;
            _allocator = allocator;

            Columns = default;
            Columns.Allocate(_allocator, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (!IsCreated)
                throw new Exception($"DatabaseTableStatic :: Dispose :: Is not created!");

            Columns.Dispose(_allocator);
        }
    }
}