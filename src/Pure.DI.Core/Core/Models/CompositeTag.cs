namespace Pure.DI.Core.Models;

using CSharp;

internal class CompositeTag
{
    public readonly ImmutableHashSet<object> Tags;

    public CompositeTag(ImmutableHashSet<object> tags) => Tags = tags;

    public override string ToString() => string.Join(", ", Tags.Select(i => i.ValueToString()));
}