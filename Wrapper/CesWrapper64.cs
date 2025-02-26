using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;

[StructLayout(LayoutKind.Sequential, Size = 64)]
public struct CesWrapper64<T> where T : unmanaged
{
    public T Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CesWrapper64(T value)
    {
        Value = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator T(CesWrapper64<T> wrap) => wrap.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator CesWrapper64<T>(T value) => new(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Serialize(in FileStream fileStream, in CesWrapper64<T> wrap)
    {
        fileStream.WriteValue(wrap.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CesWrapper32<T> Deserialize(in FileStream fileStream, Allocator allocator) => new()
    {
        Value = fileStream.ReadValue<T>(),
    };
}
