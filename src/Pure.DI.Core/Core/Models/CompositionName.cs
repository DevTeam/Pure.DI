namespace Pure.DI.Core.Models;

public readonly record struct CompositionName(string ClassName, string Namespace, SyntaxNode? Source)
{
    public bool Equals(CompositionName other) => ClassName == other.ClassName && Namespace == other.Namespace;

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