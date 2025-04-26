namespace Pure.DI.Core.Models;

record Root(
    int Index,
    DependencyNode Node,
    MdRoot Source,
    in Injection Injection,
    string Name,
    in ImmutableArray<Line> Lines,
    in ImmutableArray<Variable> Args,
    RootKinds Kind,
    TypeDescription TypeDescription = default,
    bool IsMethod = false)
{
    public bool IsPublic => !string.IsNullOrWhiteSpace(Name);

    public string DisplayName => this.GetPropertyName();

    public virtual bool Equals(Root? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Node.Equals(other.Node) && Injection.Equals(other.Injection) && Name == other.Name;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Node.GetHashCode();
            hashCode = (hashCode * 397) ^ Injection.GetHashCode();
            hashCode = (hashCode * 397) ^ Name.GetHashCode();
            return hashCode;
        }
    }
}