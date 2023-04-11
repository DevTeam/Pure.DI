namespace Pure.DI.Core.Models;

internal record DependencyNode(
    int Variation,
    DpRoot? Root = default,
    DpImplementation? Implementation = default,
    DpFactory? Factory = default,
    DpArg? Arg = default,
    DpConstruct? Construct = default)
{
    public IEnumerable<string> ToStrings(int indent) =>
        Root?.ToStrings(indent)
        ?? Implementation?.ToStrings(indent)
        ?? Factory?.ToStrings(indent)
        ?? Arg?.ToStrings(indent)
        ?? Construct?.ToStrings(indent)
        ?? Enumerable.Repeat("unresolved", 1);

    public MdBinding Binding { get; } = Root?.Binding ?? Implementation?.Binding ?? Factory?.Binding ?? Arg?.Binding ?? Construct?.Binding ?? new MdBinding();

    public ITypeSymbol Type => Root?.Source.RootType ?? Implementation?.Source.Type ?? Factory?.Source.Type ?? Arg?.Source.Type ?? Construct?.Source.Type!;
    
    public Lifetime Lifetime => Binding.Lifetime?.Value ?? Lifetime.Transient;

    public override string ToString() => string.Join(Environment.NewLine, ToStrings(0));

    public virtual bool Equals(DependencyNode? other) => 
        ReferenceEquals(this, other) || (other is not null && Binding.Equals(other.Binding));

    public override int GetHashCode() => Binding.GetHashCode();
}