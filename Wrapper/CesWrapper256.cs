using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;

[StructLayout(LayoutKind.Sequential, Size = 256)]
public struct CesWrapper256<T> where T : unmanaged
{
    public T Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CesWrapper256(T value)
    {
        Value = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator T(CesWrapper256<T> wrap) => wrap.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator CesWrapper256<T>(T value) => new(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Serialize(in FileStream fileStream, in CesWrapper256<T> wrap)
    {
        fileStream.WriteValue(wrap.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CesWrapper256<T> Deserialize(in FileStream fileStream, Allocator allocator) => new()
    {
        Value = fileStream.ReadValue<T>(),
    };
}
