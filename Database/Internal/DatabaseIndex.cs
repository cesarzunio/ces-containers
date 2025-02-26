using System.Runtime.CompilerServices;

namespace Ces.Collections
{
    public readonly struct DatabaseIndex
    {
        public const int INVALID = -1;
        public readonly int Index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DatabaseIndex(int index)
        {
            Index = index;
        }
        public static DatabaseIndex Invalid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(INVALID);
        }

        public bool IsInvalid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Index == INVALID;
        }
    }
}