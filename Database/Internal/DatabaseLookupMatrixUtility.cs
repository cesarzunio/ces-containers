using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Ces.Collections
{
    public static unsafe class DatabaseLookupMatrixUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetLookupMatrixLength(int rowsCount)
        {
            return rowsCount * (rowsCount + 1) / 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetLookupMatrixIndex(int a, int b)
        {
            int index = GetLookupMatrixLength(a) + b;
            int indexFlip = GetLookupMatrixLength(b) + a;

            return a >= b ? index : indexFlip;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetAllLookupValues<TLookup>(int a, [NoAlias] TLookup* lookupMatrix, int rowsCount, TLookup valueToSet)
            where TLookup : unmanaged
        {
            for (int i = 0; i < rowsCount; i++)
            {
                lookupMatrix[GetLookupMatrixIndex(a, i)] = valueToSet;
            }
        }
    }
}