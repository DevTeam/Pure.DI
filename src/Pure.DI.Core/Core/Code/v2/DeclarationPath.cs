using Pure.DI.Core.Code.v2;

record DeclarationPath(ImmutableArray<VarDeclaration> Declarations)
{
    public static readonly DeclarationPath Root = new(ImmutableArray<VarDeclaration>.Empty);

    public DeclarationPath Append(VarDeclaration declaration)
    {
        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (Declarations.Any(i => i.Node.Equals(declaration.Node)))
        {
            return this;
        }

        return new DeclarationPath(Declarations.Add(declaration));
    }

    public virtual bool Equals(DeclarationPath? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Declarations.Length == other.Declarations.Length
               && Declarations.Zip(other.Declarations, (declaration, otherDeclaration) => declaration.Node.Equals(otherDeclaration.Node)).All(i => i);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = 0;
            // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
            foreach (var declaration in Declarations)
            {
                hashCode = declaration.Node.GetHashCode() * 397 ^ hashCode;
            }

            return hashCode;
        }
    }
}