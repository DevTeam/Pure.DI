namespace Pure.DI.Core.Models;

internal class SyntaxTag
{
    private readonly SyntaxNode _node;

    public SyntaxTag(SyntaxNode node)
    {
        _node = node;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return _node.IsEquivalentTo(((SyntaxTag)obj)._node);
    }

    public override int GetHashCode() => 99;

    public override string ToString() => _node.ToString();
}