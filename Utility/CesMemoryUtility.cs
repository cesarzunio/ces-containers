using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

public static unsafe class CesMemoryUtility
{
    public const int CACHE_LINE_SIZE = 64;
    public const ulong CHECK_SUM_VALUE = 0xF0F1F2F3F4F5F6F7ul;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T* Allocate<T>(long length, int alignment, Allocator allocator) where T : unmanaged
    {
        return (T*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<T>() * length, math.max(alignment, UnsafeUtility.AlignOf<T>()), allocator);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T* AllocateCache<T>(long length, Allocator allocator) where T : unmanaged
    {
        return Allocate<T>(length, CACHE_LINE_SIZE, allocator);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T* AllocateDefault<T>(long length, int alignment, Allocator allocator, T valueDefault) where T : unmanaged
    {
        var ptr = Allocate<T>(length, alignment, allocator);
        MemSet(ptr, valueDefault, length);
        return ptr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T* AllocateCacheDefault<T>(long length, Allocator allocator, T valueDefault) where T : unmanaged
    {
        var ptr = AllocateCache<T>(length, allocator);
        MemSet(ptr, valueDefault, length);
        return ptr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T** AllocatePtrs<T>(long length, Allocator allocator) where T : unmanaged
    {
        long sizePtr = UnsafeUtility.SizeOf<IntPtr>() * length;
        int alignOfPtr = UnsafeUtility.AlignOf<IntPtr>();

        return (T**)UnsafeUtility.Malloc(sizePtr, alignOfPtr, allocator);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Copy<T>(long length, T* destination, T* source) where T : unmanaged
    {
        long size = UnsafeUtility.SizeOf<T>() * length;

        UnsafeUtility.MemCpy(destination, source, size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyAndFree<T>(long length, T* destination, T* source, Allocator allocator) where T : unmanaged
    {
        Copy(length, destination, source);
        UnsafeUtility.Free(source, allocator);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ShiftLeftByOne<T>(T* destination, long elementsToMove) where T : unmanaged
    {
        long size = UnsafeUtility.SizeOf<T>() * elementsToMove;
        UnsafeUtility.MemMove(destination, destination + 1, size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ShiftRightByOne<T>(T* destination, long elementsToMove) where T : unmanaged
    {
        long size = UnsafeUtility.SizeOf<T>() * elementsToMove;
        UnsafeUtility.MemMove(destination, destination - 1, size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FreeAndNullify<T>(ref T* ptr, Allocator allocator) where T : unmanaged
    {
        UnsafeUtility.Free(ptr, allocator);
        ptr = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FreeAndNullify<T>(ref T** ptr, Allocator allocator) where T : unmanaged
    {
        UnsafeUtility.Free(ptr, allocator);
        ptr = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetSafeSize(int stride, long length)
    {
        if (stride <= 0)
            throw new Exception($"BinaryUtility :: GetSafeSizeT :: Stride ({stride}) is lower than 0!");

        long sizeT = stride * length;

        if (sizeT > int.MaxValue)
            throw new Exception($"BinaryUtility :: GetSafeSizeT :: SizeT ({sizeT}) exceeds int.MaxValue!");

        return (int)sizeT;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void MemSet<T>(T* array, T value, long length) where T : unmanaged
    {
        for (int i = 0; i < length; i++)
        {
            array[i] = value;
        }
    }
}
