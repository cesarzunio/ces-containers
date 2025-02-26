using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ces.Collections
{
    public unsafe interface IDatabaseIdData
    {
        int GetCapacity();
        DatabaseIndex* GetIdToIndex();
        uint* GetIdToUseCount();
    }
}