namespace Pure.DI.Core.Models;

internal readonly record struct DependencyNode(
    int Variation,
    in MdBinding Binding,
    ITypeSymbol Type,
    in DpRoot? Root = default,
    in DpImplementation? Implementation = default,
    in DpFactory? Factory = default,
    in DpArg? Arg = default,
    in DpConstruct? Construct = default)
{
    public DependencyNode(
        int Variation,
        in DpRoot? Root = default,
        in DpImplementation? Implementation = default,
        in DpFactory? Factory = default,
        in DpArg? Arg = default,
        in DpConstruct? Construct = default)
        :this(
            Variation,
            Root?.Binding ?? Implementation?.Binding ?? Factory?.Binding ?? Arg?.Binding ?? Construct?.Binding ?? new MdBinding(),
            Root?.Source.RootType ?? Implementation?.Source.Type ?? Factory?.Source.Type ?? Arg?.Source.Type ?? Construct?.Source.Type!,
            Root,
            Implementation,
            Factory,
            Arg,
            Construct)
    {
    }
        
    private IEnumerable<string> ToStrings(int indent) =>
        Root?.ToStrings(indent)
        ?? Implementation?.ToStrings(indent)
        ?? Factory?.ToStrings(indent)
        ?? Arg?.ToStrings(indent)
        ?? Construct?.ToStrings(indent)
        ?? Enumerable.Repeat("unresolved", 1);

    public Lifetime Lifetime => Binding.Lifetime?.Value ?? Lifetime.Transient;

    public override string ToString() => string.Join(Environment.NewLine, ToStrings(0));

    public bool Equals(DependencyNode other) => Binding.Equals(other.Binding);

    public override int GetHashCode() => Binding.GetHashCode();
}