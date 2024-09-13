namespace Pure.DI.Core.Models;

public readonly record struct CompositionName(string ClassName, string Namespace, SyntaxNode? Source) : IComparable<CompositionName>
{
    public bool Equals(CompositionName other) => ClassName == other.ClassName && Namespace == other.Namespace;

    public int CompareTo(CompositionName other) =>
        Comparer.DefaultInvariant.Compare(FullName, other.FullName);

    public override int GetHashCode()
    {
        unchecked
        {
            return (ClassName.GetHashCode() * 397) ^ Namespace.GetHashCode();
        }
    }

    public string FullName =>
        string.IsNullOrWhiteSpace(Namespace) ? ClassName : Namespace + "." + ClassName;

    public override string ToString() => FullName;
}