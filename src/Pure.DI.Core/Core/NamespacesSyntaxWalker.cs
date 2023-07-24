namespace Pure.DI.Core;

using System.Collections;

internal class NamespacesSyntaxWalker: CSharpSyntaxWalker, IEnumerable<string>
{
    private readonly SemanticModel _semanticModel;
    private readonly HashSet<string> _namespaces = new();

    public NamespacesSyntaxWalker(SemanticModel semanticModel)
    {
        _semanticModel = semanticModel;
    }

    public override void VisitIdentifierName(IdentifierNameSyntax node)
    {
        TryAddNamespace(node);
        base.VisitIdentifierName(node);
    }

    public override void VisitGenericName(GenericNameSyntax node)
    {
        TryAddNamespace(node);
        base.VisitGenericName(node);
    }

    private void TryAddNamespace(ExpressionSyntax node)
    {
        var symbol = _semanticModel.GetSymbolInfo(node).Symbol;
        if (symbol is not INamedTypeSymbol type)
        {
            return;
        }

        if (type.ContainingNamespace.IsGlobalNamespace)
        {
            return;
        }

        _namespaces.Add(type.ContainingNamespace.ToString());
    }

    public IEnumerator<string> GetEnumerator() => _namespaces.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}