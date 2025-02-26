using System.Runtime.CompilerServices;

namespace Ces.Collections
{
    public readonly struct DatabaseMulIndex
    {
        public const int INVALID = -1;
        public readonly int TableIndex;
        public readonly int Index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DatabaseMulIndex(int tableIndex, int index)
        {
            TableIndex = tableIndex;
            Index = index;
        }

        public static DatabaseMulIndex Invalid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(INVALID, INVALID);
        }

        public bool IsInvalid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => TableIndex == INVALID;
        }
    }
}