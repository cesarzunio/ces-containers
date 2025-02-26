using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Ces.Collections
{
    public interface IDatabaseColumns<TInstance, TColumns>
        where TInstance : unmanaged
        where TColumns : unmanaged
    {
        bool IsCreated();

        void Allocate(Allocator allocator, int capacity);
        void Dispose(Allocator allocator);

        TInstance Get(int index);
        void Set(int index, TInstance instance);

        void Move(int from, int to);
        void Copy(in TColumns from, int capacity);
    }
}