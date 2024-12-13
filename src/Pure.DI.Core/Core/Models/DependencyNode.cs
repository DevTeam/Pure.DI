namespace Pure.DI.Core.Models;

internal record DependencyNode(
    int Variation,
    in MdBinding Binding,
    ITypeSymbol Type,
    ICollection<Accumulator> Accumulators,
    in DpRoot? Root,
    in DpImplementation? Implementation,
    in DpFactory? Factory,
    in DpArg? Arg,
    in DpConstruct? Construct,
    Lifetime Lifetime)
{
    public DependencyNode(
        int Variation,
        in MdBinding binding,
        in DpRoot? Root = null,
        in DpImplementation? Implementation = null,
        in DpFactory? Factory = null,
        in DpArg? Arg = null,
        in DpConstruct? Construct = null)
        : this(
            Variation,
            binding,
            Root?.Source.RootType ?? Implementation?.Source.Type ?? Factory?.Source.Type ?? Arg?.Source.Type ?? Construct?.Source.Type!,
            [],
            Root,
            Implementation,
            Factory,
            Arg,
            Construct,
            binding.Lifetime?.Value ?? Lifetime.Transient)
    {
    }

    private IEnumerable<string> ToStrings(int indent) =>
        Root?.ToStrings(indent)
        ?? Implementation?.ToStrings(indent)
        ?? Factory?.ToStrings(indent)
        ?? Arg?.ToStrings(indent)
        ?? Construct?.ToStrings(indent)
        ?? Enumerable.Repeat("unresolved", 1);

    public override string ToString() => string.Join(Environment.NewLine, ToStrings(0));

    public virtual bool Equals(DependencyNode? other) => Binding.Equals(other?.Binding);

    public override int GetHashCode() => Binding.GetHashCode();
}