// ReSharper disable HeapView.ObjectAllocation

namespace Pure.DI.Core.Models;

readonly record struct MdDefaultLifetime(
    in MdLifetime Lifetime,
    ITypeSymbol? Type = null,
    ImmutableArray<MdTag> Tags = default)
{
    public override string ToString() =>
        $"DefaultLifetime{(Type is null ? "" : $"<{Type}>")}({Lifetime.Value}{(Tags.IsDefaultOrEmpty ? "" : string.Join(", ", Tags.Select(i => i.ValueToString())))})";
}