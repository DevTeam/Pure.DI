// ReSharper disable ForCanBeConvertedToForeach
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal class BytesArrayEqualityComparer: IEqualityComparer<ImmutableArray<byte>>
{
    public int GetHashCode(ImmutableArray<byte> value)
    {
        var hashCode = 397;
        for (var i = 0; i < value.Length; i++)
        {
            hashCode = HashCode.Combine(hashCode, value.ItemRef(i));
        }

        return hashCode;
    }

    public bool Equals(ImmutableArray<byte> x, ImmutableArray<byte> y)
    {
        if (x.Length != y.Length)
        {
            return false;
        }

        for (var i = 0; i < x.Length; i++)
        {
            if (x.ItemRef(i) != y.ItemRef(i))
            {
                return false;
            }
        }

        return true;
    }
}