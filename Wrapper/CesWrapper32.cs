using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;

[StructLayout(LayoutKind.Sequential, Size = 32)]
public struct CesWrapper32<T> where T : unmanaged
{
    public T Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CesWrapper32(T value)
    {
        Value = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator T(CesWrapper32<T> wrap) => wrap.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator CesWrapper32<T>(T value) => new(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Serialize(in FileStream fileStream, in CesWrapper32<T> wrap)
    {
        fileStream.WriteValue(wrap.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CesWrapper32<T> Deserialize(in FileStream fileStream, Allocator allocator) => new()
    {
        Value = fileStream.ReadValue<T>(),
    };
}
