using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ces.Collections
{
    public unsafe interface IDatabaseMulIdData
    {
        int GetCapacity();
        DatabaseMulIndex* GetIdToIndex();
        uint* GetIdToUseCount();
    }
}