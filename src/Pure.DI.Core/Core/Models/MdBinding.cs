namespace Pure.DI.Core.Models;

internal record MdBinding(
    int Id,
    in SyntaxNode Source,
    in MdSetup SourceSetup,
    in SemanticModel SemanticModel,
    in ImmutableArray<MdContract> Contracts,
    in ImmutableArray<MdTag> Tags,
    in MdLifetime? Lifetime = default,
    in MdImplementation? Implementation = default,
    in MdFactory? Factory = default,
    in MdArg? Arg = default,
    in MdConstruct? Construct = default,
    ITypeConstructor? TypeConstructor = default)
{
    public override string ToString()
    {
        var walker = new MetadataToLinesWalker(0);
        walker.VisitBinding(this);
        return string.Join(Environment.NewLine, walker);
    }

    public virtual bool Equals(MdBinding? other) => Id == other?.Id;

    public override int GetHashCode() => Id;
}