namespace Pure.DI.Core.Models;

readonly record struct CompositionName(string ClassName, string Namespace, SyntaxNode? Source) : IComparable<CompositionName>
{

    public string FullName =>
        string.IsNullOrWhiteSpace(Namespace) ? ClassName : Namespace + "." + ClassName;

    public int CompareTo(CompositionName other) =>
        Comparer.DefaultInvariant.Compare(FullName, other.FullName);
    public bool Equals(CompositionName other) => ClassName == other.ClassName && Namespace == other.Namespace;

    public override int GetHashCode()
    {
        unchecked
        {
            return ClassName.GetHashCode() * 397 ^ Namespace.GetHashCode();
        }
    }

    public override string ToString() => FullName;
}