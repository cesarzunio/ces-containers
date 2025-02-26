using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using UnityEngine;
using Unity.VisualScripting;
using Ces.Collections;
using Unity.Mathematics;

#pragma warning disable IDE0017

public static unsafe class BinaryReadUtility
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T ReadValue<T>(this FileStream fileStream) where T : unmanaged
    {
        int sizeOfT = UnsafeUtility.SizeOf<T>();
        var array = stackalloc byte[sizeOfT];

        var span = new Span<byte>(array, sizeOfT);
        int bytesRead = fileStream.Read(span);

        if (bytesRead != sizeOfT)
        {
            throw new Exception("SaveUtility :: ReadValue :: Wrong number of bytes read!");
        }

        return *(T*)array;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ReadArraySimple<T>(in FileStream fileStream, T* array, int length) where T : unmanaged
    {
        if (length <= 0)
            return;

        int sizeOfT = UnsafeUtility.SizeOf<T>();
        int size = CesMemoryUtility.GetSafeSize(sizeOfT, length);

        var span = new Span<byte>(array, size);
        int bytesRead = fileStream.Read(span);

        if (bytesRead != size)
            throw new Exception("BinaryReadUtility :: ReadArraySimple :: Wrong number of bytes read!");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RawArray<T> ReadRawArray<T>(in FileStream fileStream, Allocator allocator, int alignment) where T : unmanaged
    {
        int length = fileStream.ReadValue<int>();
        var array = new RawArray<T>(allocator, length, alignment);

        ReadArraySimple(in fileStream, array.Data, length);

        return array;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RawSet<T> ReadRawSet<T>(in FileStream fileStream, Allocator allocator, int capacityMin, int alignment) where T : unmanaged
    {
        int length = fileStream.ReadValue<int>();
        int capacity = math.max(capacityMin, length);
        var set = new RawSet<T>(allocator, capacity, alignment);

        ReadArraySimple(in fileStream, set.Data, length);
        set.Count = length;

        return set;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ReadArraySimpleOfRawArrays<T>(in FileStream fileStream, RawArray<T>* array, int length, Allocator allocator, int alignment) where T : unmanaged
    {
        if (length <= 0)
            return; // should throw maybe ?

        for (int i = 0; i < length; i++)
        {
            array[i] = ReadRawArray<T>(fileStream, allocator, alignment);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static RawArray<RawArray<T>> ReadRawArrayOfRawArrays<T>(in FileStream fileStream, Allocator allocator, int alignmentOuter, int alignmentInner) where T : unmanaged
    {
        int length = fileStream.ReadValue<int>();
        var array = new RawArray<RawArray<T>>(allocator, length, alignmentOuter);

        for (int i = 0; i < length; i++)
        {
            array[i] = ReadRawArray<T>(fileStream, allocator, alignmentInner);
        }

        return array;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ReadArraySimpleOfRawSets<T>(in FileStream fileStream, RawSet<T>* array, int length, Allocator allocator, int capacityMin, int alignment)
        where T : unmanaged
    {
        if (length <= 0)
            return; // should throw maybe ?

        for (int i = 0; i < length; i++)
        {
            array[i] = ReadRawSet<T>(fileStream, allocator, capacityMin, alignment);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ReadArraySimpleOfSerializables<T>(in FileStream fileStream, T* array, int length, Allocator allocator, delegate*<in FileStream, Allocator, T> deserializeFunc)
        where T : unmanaged
    {
        if (length <= 0)
            return; // should throw maybe ?

        for (int i = 0; i < length; i++)
        {
            array[i] = deserializeFunc(fileStream, allocator);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCheckSumInvalid(in FileStream fileStream)
    {
        return fileStream.ReadValue<ulong>() != CesMemoryUtility.CHECK_SUM_VALUE;
    }
}
