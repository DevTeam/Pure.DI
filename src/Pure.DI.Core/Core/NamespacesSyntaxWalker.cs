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
        AddNamespace(_semanticModel.GetSymbolInfo(node).Symbol);
        base.VisitIdentifierName(node);
    }

    public override void VisitGenericName(GenericNameSyntax node)
    {
        AddNamespace(_semanticModel.GetSymbolInfo(node).Symbol);
        base.VisitGenericName(node);
    }

    private void AddNamespace(ISymbol? symbol)
    {
        if (symbol is not INamedTypeSymbol type)
        {
            return;
        }

        if (type.ContainingNamespace.IsGlobalNamespace)
        {
            return;
        }

        _namespaces.Add(type.ContainingNamespace.ToString());
        if (!type.IsGenericType)
        {
            return;
        }
        
        foreach (var typeSymbol in type.TypeArguments)
        {
            AddNamespace(typeSymbol);
        }
    }

    public IEnumerator<string> GetEnumerator() => _namespaces.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}