using System;
using System.Runtime.CompilerServices;

#pragma warning disable CS0660
#pragma warning disable CS0661

namespace Ces.Collections
{
    public readonly struct DatabaseId : IEquatable<DatabaseId>
    {
        const int INVALID = -1;
        public readonly int Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DatabaseId(int value)
        {
            Value = value;
        }

        public static DatabaseId Invalid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(INVALID);
        }

        public readonly bool IsInvalid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Value == INVALID;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(DatabaseId other)
        {
            return Value == other.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(DatabaseId lhs, DatabaseId rhs)
        {
            return lhs.Value == rhs.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(DatabaseId lhs, DatabaseId rhs)
        {
            return !(lhs.Value == rhs.Value);
        }
    }
}