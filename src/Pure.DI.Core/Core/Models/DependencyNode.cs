namespace Pure.DI.Core.Models;

internal record DependencyNode(
    DpRoot? Root = default,
    DpImplementation? Implementation = default,
    DpFactory? Factory = default,
    DpArg? Arg = default)
{
    public IEnumerable<string> ToStrings(int indent) =>
        Root?.ToStrings(indent)
        ?? Implementation?.ToStrings(indent)
        ?? Factory?.ToStrings(indent)
        ?? Arg?.ToStrings(indent)
        ?? Enumerable.Repeat("unresolved", 1);

    public MdBinding Binding { get; } = Root?.Binding ?? Implementation?.Binding ?? Factory?.Binding ?? Arg?.Binding ?? new MdBinding();

    public ITypeSymbol Type => Root?.Source.RootType ?? Implementation?.Source.Type ?? Factory?.Source.Type ?? Arg?.Source.Type!;

    public Lifetime Lifetime => Binding.Lifetime?.Lifetime is Lifetime lifetime ? lifetime : Lifetime.Transient;

    public override string ToString() => string.Join(Environment.NewLine, ToStrings(0));

    public virtual bool Equals(DependencyNode? other) => Binding.Equals(other?.Binding);

    public override int GetHashCode() => Binding.GetHashCode();
}