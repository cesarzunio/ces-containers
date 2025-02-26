using System;
using System.IO;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor.UIElements;
using Ces.Collections;
using UnityEngine.UI;

public static unsafe class BinarySaveUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteValue<T>(this FileStream fileStream, T value) where T : unmanaged
    {
        int sizeOfT = UnsafeUtility.SizeOf<T>();
        var array = stackalloc byte[sizeOfT];

        *(T*)array = value;

        fileStream.Write(new ReadOnlySpan<byte>(array, sizeOfT));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteArraySimple<T>(in FileStream fileStream, T* array, int length) where T : unmanaged
    {
        if (length <= 0)
            return;

        int size = CesMemoryUtility.GetSafeSize(UnsafeUtility.SizeOf<T>(), length);
        var span = new ReadOnlySpan<byte>(array, size);

        fileStream.Write(span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteRawArray<T>(in FileStream fileStream, in RawArray<T> rawArray) where T : unmanaged
    {
        int length = rawArray.Length;
        var data = rawArray.Data;

        fileStream.WriteValue(length);

        WriteArraySimple(fileStream, data, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteRawSet<T>(in FileStream fileStream, in RawSet<T> rawSet) where T : unmanaged
    {
        int length = rawSet.Count;
        var data = rawSet.Data;

        fileStream.WriteValue(length);

        WriteArraySimple(fileStream, data, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteRawList<T>(in FileStream fileStream, in RawList<T> rawList) where T : unmanaged
    {
        int length = rawList.Count;
        var data = rawList.Data;

        fileStream.WriteValue(length);

        WriteArraySimple(fileStream, data, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteRawArrayOfRawArrays<T>(in FileStream fileStream, RawArray<RawArray<T>> rawArrayOfRawArrays) where T : unmanaged
    {
        fileStream.WriteValue(rawArrayOfRawArrays.Length);

        WriteArraySimpleOfRawArrays(fileStream, rawArrayOfRawArrays.Data, rawArrayOfRawArrays.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteArraySimpleOfRawArrays<T>(in FileStream fileStream, RawArray<T>* arrayOfRawArrays, int length) where T : unmanaged
    {
        for (int i = 0; i < length; i++)
        {
            WriteRawArray(fileStream, arrayOfRawArrays[i]);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteArraySimpleOfRawSets<T>(in FileStream fileStream, RawSet<T>* arrayOfRawSets, int length) where T : unmanaged
    {
        for (int i = 0; i < length; i++)
        {
            WriteRawSet(fileStream, arrayOfRawSets[i]);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteArraySimpleOfSerializables<T>(in FileStream fileStream, T* array, int length, delegate*<in FileStream, in T, void> serializeFunc)
        where T : unmanaged
    {
        for (int i = 0; i < length; i++)
        {
            serializeFunc(in fileStream, in array[i]);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteCheckSum(in FileStream fileStream)
    {
        fileStream.WriteValue(CesMemoryUtility.CHECK_SUM_VALUE);
    }
}