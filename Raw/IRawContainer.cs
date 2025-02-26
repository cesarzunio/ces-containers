namespace Ces.Collections
{
    public unsafe interface IRawContainer<T> where T : unmanaged
    {
        T* GetData();
        int GetLength();
    }
}