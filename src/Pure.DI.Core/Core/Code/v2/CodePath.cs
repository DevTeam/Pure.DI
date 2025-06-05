namespace Pure.DI.Core.Code.v2;

class CodePath : LinkedList<VarDeclaration>
{
    public static readonly CodePath Root = [];

    private CodePath() {}

    private CodePath(IEnumerable<VarDeclaration> collection): base(collection) {}

    public CodePath Append(VarDeclaration blockDeclaration)
    {
        var newPath = new CodePath(this);
        newPath.AddLast(blockDeclaration);
        return newPath;
    }

    private bool Equals(CodePath other) =>
        Count == other.Count
        && this
            .Zip(other, (declaration, otherDeclaration) => declaration.Name.Equals(otherDeclaration.Name))
            .All(i => i);

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (obj.GetType() != GetType()) return false;
        return Equals((CodePath)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = 0;
            // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
            foreach (var path in this)
            {
                hashCode = hashCode * 397 ^ path.GetHashCode();
            }

            return hashCode;
        }
    }

    public override string ToString() => string.Join(" - ", this);
}