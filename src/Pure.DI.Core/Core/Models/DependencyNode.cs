namespace Pure.DI.Core.Models;

record DependencyNode(
    int Variation,
    in MdBinding Binding,
    ITypeConstructor TypeConstructor,
    ITypeSymbol Type,
    in DpRoot? Root,
    in DpImplementation? Implementation,
    in DpFactory? Factory,
    in DpArg? Arg,
    in DpConstruct? Construct,
    Lifetime Lifetime,
    CompileErrorException? Error)
    : IDependencyNode
{
    public DependencyNode(
        int Variation,
        in MdBinding binding,
        ITypeConstructor typeConstructor,
        in DpRoot? Root = null,
        in DpImplementation? Implementation = null,
        in DpFactory? Factory = null,
        in DpArg? Arg = null,
        in DpConstruct? Construct = null,
        CompileErrorException? Error = null)
        : this(
            Variation,
            binding,
            typeConstructor,
            Root?.Source.RootType ?? binding.Type,
            Root,
            Implementation,
            Factory,
            Arg,
            Construct,
            binding.Lifetime?.Value ?? Lifetime.Transient,
            Error)
    {
    }

    public virtual bool Equals(DependencyNode? other) => Binding.Equals(other?.Binding);

    private IEnumerable<string> ToStrings(int indent) =>
        Root?.ToStrings(indent)
        ?? Implementation?.ToStrings(indent)
        ?? Factory?.ToStrings(indent)
        ?? Arg?.ToStrings(indent)
        ?? Construct?.ToStrings(indent)
        ?? Enumerable.Repeat("unresolved", 1);

    public override string ToString() => string.Join(Environment.NewLine, ToStrings(0));

    public override int GetHashCode() => Binding.GetHashCode();

    public int BindingId => Binding.Id;

    public DependencyNode Node => this;
}