namespace Pure.DI.Core;

class PathKey
{
    public static readonly PathKey Default = new(ImmutableArray<int>.Empty);
    private readonly ImmutableArray<int> _path;
    private readonly int _hashCode;

    public PathKey(in ImmutableArray<int> path)
    {
        _path = path;
        var hashCode = 0;
        foreach (var id in path)
        {
            unchecked
            {
                hashCode = hashCode * 397 ^ id;
            }
        }

        _hashCode = hashCode;
    }

    // ReSharper disable once MemberCanBePrivate.Global
    protected bool Equals(PathKey other) =>
        _path.AsSpan().SequenceEqual(other._path.AsSpan());

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals((PathKey)obj);
    }

    public override int GetHashCode() => _hashCode;
}