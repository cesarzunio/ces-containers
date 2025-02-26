using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Ces.Collections
{
    public readonly struct DatabaseMulIdIndexPair
    {
        public readonly DatabaseId Id;
        public readonly DatabaseMulIndex Index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DatabaseMulIdIndexPair(DatabaseId id, DatabaseMulIndex index)
        {
            Id = id;
            Index = index;
        }
    }
}