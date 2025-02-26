using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Ces.Collections
{
    [NoAlias]
    public unsafe struct RawStackStackalloc<T> where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction, NoAlias]
        public readonly T* Stack;

        public int Count;
        public readonly int Capacity;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RawStackStackalloc(T* stackPtr, int capacity)
        {
            Stack = stackPtr;
            Count = 0;
            Capacity = capacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T value)
        {
#if CES_COLLECTIONS_CHECK
        if (Count == Capacity)
            throw new Exception("RawStackStackalloc :: Add :: Stack is full ({_capacity})!");
#endif

            Stack[Count++] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPop(out T value)
        {
            if (Count == 0)
            {
                value = default;
                return false;
            }

            value = Pop();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Pop()
        {
#if CES_COLLECTIONS_CHECK
        if (Count == 0)
            throw new Exception("RawStackStackalloc :: Add :: Stack is empty!");
#endif

            return Stack[--Count];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            Count = 0;
        }
    }
}