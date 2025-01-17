// ReSharper disable HeapView.ObjectAllocation
// ReSharper disable HeapView.BoxingAllocation
// ReSharper disable HeapView.ObjectAllocation.Evident

namespace Pure.DI.Core.Models;

internal record MdSetup(
    SemanticModel SemanticModel,
    SyntaxNode Source,
    in CompositionName Name,
    in ImmutableArray<MdUsingDirectives> UsingDirectives,
    CompositionKind Kind,
    IHints Hints,
    in ImmutableArray<MdBinding> Bindings,
    in ImmutableArray<MdRoot> Roots,
    in ImmutableArray<MdDependsOn> DependsOn,
    ImmutableArray<MdGenericTypeArgument> GenericTypeArguments,
    ImmutableArray<MdGenericTypeArgumentAttribute> GenericTypeArgumentAttributes,
    in ImmutableArray<MdTypeAttribute> TypeAttributes,
    in ImmutableArray<MdTagAttribute> TagAttributes,
    in ImmutableArray<MdOrdinalAttribute> OrdinalAttributes,
    in ImmutableArray<MdAccumulator> Accumulators,
    IReadOnlyCollection<MdTagOnSites> TagOn,
    IReadOnlyCollection<string> Comments)
{
    public virtual bool Equals(MdSetup? other) => 
        other is not null && (ReferenceEquals(this, other) || Name.Equals(other.Name));

    public override int GetHashCode() => Name.GetHashCode();
}