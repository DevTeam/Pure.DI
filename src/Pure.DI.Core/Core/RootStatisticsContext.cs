namespace Pure.DI.Core;

class RootStatisticsContext
{
    private static readonly int LifetimesSize = Enum.GetValues(typeof(Lifetime)).Length;
    private int[] _lifetimesCount = new int[LifetimesSize];

    public void RegisterNode(DependencyNode node)
    {
        ref var count = ref _lifetimesCount[(int)node.ActualLifetime];
        count++;
    }

    public int GetNodeCountByLifetime(Lifetime lifetime) =>
        _lifetimesCount[(int)lifetime];
}