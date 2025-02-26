using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Ces.Collections
{
    public readonly struct DatabaseIdIndexPair
    {
        public readonly DatabaseId Id;
        public readonly int Index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DatabaseIdIndexPair(DatabaseId id, int index)
        {
            Id = id;
            Index = index;
        }
    }
}