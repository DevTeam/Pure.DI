// ReSharper disable HeapView.ObjectAllocation.Evident
namespace Pure.DI.Core;

using System.Text;

internal static class ImmutableArrayExtensions
{
    public static void ToBuilder<TSrc, TDst>(
        this in ImmutableArray<TSrc> items,
        ImmutableArray<TDst>.Builder builder,
        Func<TSrc, TDst> transformation)
    {
        foreach (var item in items)
        {
            builder.Add(transformation(item));
        }
    }
    
    
    public static ImmutableArray<T> SafeMoveToImmutable<T>(this ImmutableArray<T>.Builder builder) => 
        builder.Capacity == builder.Count
            ? builder.MoveToImmutable()
            : builder.ToImmutable();

    public static string Join(this ImmutableArray<string>.Builder builder, string separator) =>
        builder.SafeMoveToImmutable().Join(separator);

    public static string Join(this in ImmutableArray<string> items, string separator)
    {
        var sb = new StringBuilder();
        var isFirst = true;
        foreach (var item in items)
        {
            if (isFirst)
            {
                isFirst = false;
            }
            else
            {
                sb.Append(separator);
            }
            
            sb.Append(item);
        }

        return sb.ToString();
    }
}