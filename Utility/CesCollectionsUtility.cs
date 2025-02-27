using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEngine;
using Ces.Collections;
using Unity.Mathematics;

namespace Ces.Collections
{
    public static unsafe class CesCollectionsUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsOutOfRange(int index, int capacity)
        {
            return index < 0 || index >= capacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsOutOfRange(uint index, int capacity)
        {
            return (int)index >= capacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsOutOfRange(int2 pixelCoord, int2 textureSize)
        {
            return pixelCoord.x < 0 || pixelCoord.x >= textureSize.x || pixelCoord.y < 0 || pixelCoord.y >= textureSize.y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ExceedsCapacity(int length, int capacity)
        {
            return length < 0 || length > capacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CapacityUp(int capacityCurrent)
        {
            return capacityCurrent * 2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int CapacityInitialAligned(int capacityRequested, int capacityMin)
        {
            capacityRequested = math.max(capacityRequested, capacityMin);
            capacityRequested = math.ceilpow2(capacityRequested);
        
            return capacityRequested;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DisposeDeep<T>(this ref RawArray<T> rawArray) where T : unmanaged, IDisposable
        {
            if (!rawArray.IsCreated)
            {
#if CES_COLLECTIONS_WARNING
                Debug.LogError("CesCollectionsUtility :: DisposeDeep :: RawArray is not created!");
#endif

                return;
            }

            for (int i = 0; i < rawArray.Length; i++)
            {
                rawArray[i].Dispose();
            }

            rawArray.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DisposeDeep<T>(this ref RawSet<T> rawSet) where T : unmanaged, IDisposable
        {
            if (!rawSet.IsCreated)
            {
#if CES_COLLECTIONS_WARNING
                Debug.LogError("CesCollectionsUtility :: DisposeDeep :: RawSet is not created!");
#endif

                return;
            }

            for (int i = 0; i < rawSet.Count; i++)
            {
                rawSet[i].Dispose();
            }

            rawSet.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DisposeDeep<T>(this ref RawList<T> rawList) where T : unmanaged, IDisposable
        {
            if (!rawList.IsCreated)
            {
#if CES_COLLECTIONS_WARNING
                Debug.LogError("CesCollectionsUtility :: DisposeDeep :: RawList is not created!");
#endif

                return;
            }

            for (int i = 0; i < rawList.Count; i++)
            {
                rawList[i].Dispose();
            }

            rawList.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DisposeArraySimpleOfDisposables<T>(T* array, int length) where T : unmanaged, IDisposable
        {
            if (array == null)
            {
#if CES_COLLECTIONS_WARNING
                Debug.LogError("CesCollectionsUtility :: DisposeArraySimpleOfDisposables :: Array is null!");
#endif

                return;
            }

            for (int i = 0; i < length; i++)
            {
                array[i].Dispose();
            }
        }
    }
}