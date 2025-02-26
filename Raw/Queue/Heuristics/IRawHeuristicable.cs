namespace Ces.Collections
{
    public interface IRawHeuristicable<T> where T : unmanaged
    {
        double CalculateHeuristic(T item);
    }
}