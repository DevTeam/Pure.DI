namespace Pure.DI.Core;

internal sealed class NamespacesWalker(ISemantic semantic)
    : CSharpSyntaxWalker, INamespacesWalker
{
    private readonly HashSet<string> _namespaces = [];
    private SemanticModel? _semanticModel;

    public INamespacesWalker Initialize(SemanticModel semanticModel)
    {
        _semanticModel = semanticModel;
        return this;
    }

    public IReadOnlyCollection<string> GetResult() => _namespaces;

    public override void VisitIdentifierName(IdentifierNameSyntax node)
    {
        AddNamespace(_semanticModel?.GetSymbolInfo(node).Symbol);
        base.VisitIdentifierName(node);
    }

    public override void VisitGenericName(GenericNameSyntax node)
    {
        AddNamespace(_semanticModel?.GetSymbolInfo(node).Symbol);
        base.VisitGenericName(node);
    }

    private void AddNamespace(ISymbol? symbol)
    {
        var depth = 256;
        while (depth-- > 0)
        {
            switch (symbol)
            {
                case INamedTypeSymbol type:
                {
                    if (semantic.IsValidNamespace(type.ContainingNamespace))
                    {
                        _namespaces.Add(type.ContainingNamespace.ToString());
                    }

                    if (!type.IsGenericType)
                    {
                        return;
                    }

                    foreach (var typeSymbol in type.TypeArguments)
                    {
                        AddNamespace(typeSymbol);
                    }

                    break;
                }

                case IMethodSymbol methodSymbol:
                    symbol = methodSymbol.ContainingSymbol;
                    continue;
            }

            break;
        }
    }
}