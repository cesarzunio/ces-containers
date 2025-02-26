using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct RawParams<T> where T : unmanaged
{
    public RawParamsFlag Flag;

    public T ValueDefault;
}

public enum RawParamsFlag : uint
{
    None = 0,

    ClearMemory = 1u << 0,
    SetValueDefault = 1u << 1,


}
