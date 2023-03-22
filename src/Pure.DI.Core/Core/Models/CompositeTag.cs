namespace Pure.DI.Core.Models;

internal class CompositeTag
{
    public readonly ImmutableHashSet<object> Tags;

    public CompositeTag(ImmutableHashSet<object> tags) => Tags = tags;
}