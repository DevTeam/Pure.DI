// ReSharper disable InvertIf

namespace Pure.DI.Core.Code;

using System.Collections.Concurrent;

sealed class LocalVariableRenamingRewriter(
    INameProvider nameProvider)
    : CSharpSyntaxRewriter, ILocalVariableRenamingRewriter
{
    private ConcurrentDictionary<string, string> Names { get; init; } = [];
    private readonly HashSet<ISymbol> _renamedParameters = new(SymbolEqualityComparer.Default);
    private bool _isOverride;
    private SemanticModel? _semanticModel;
    private SyntaxNode? _root;

    public SyntaxNode Rewrite(SemanticModel semanticModel, bool isOverride, SyntaxNode lambda)
    {
        _semanticModel = semanticModel;
        _isOverride = isOverride;
        _root = lambda;
        return Visit(lambda);
    }

    public ILocalVariableRenamingRewriter Clone() =>
        new LocalVariableRenamingRewriter(nameProvider) { Names = new ConcurrentDictionary<string, string>(Names) };

    public override SyntaxNode? VisitVariableDeclarator(VariableDeclaratorSyntax node) =>
        base.VisitVariableDeclarator(node.WithIdentifier(SyntaxFactory.Identifier(GetUniqueName(node.Identifier.Text))));

    public override SyntaxNode? VisitSingleVariableDesignation(SingleVariableDesignationSyntax node) =>
        base.VisitSingleVariableDesignation(node.WithIdentifier(SyntaxFactory.Identifier(GetUniqueName(node.Identifier.Text))));

    public override SyntaxNode? VisitParameter(ParameterSyntax node)
    {
        if (!Names.ContainsKey(node.Identifier.Text))
        {
            return base.VisitParameter(node);
        }

        if (_semanticModel?.SyntaxTree == node.SyntaxTree && _semanticModel.GetDeclaredSymbol(node) is {} parameter)
        {
            _renamedParameters.Add(parameter);
        }

        return base.VisitParameter(node.WithIdentifier(SyntaxFactory.Identifier(GetUniqueName(node.Identifier.Text)).WithTriviaFrom(node.Identifier)));
    }

    public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
    {
        if (node.Parent is MemberAccessExpressionSyntax memberAccessExpression && memberAccessExpression.Expression != node)
        {
            return node;
        }

        return base.VisitIdentifierName(_isOverride && IsLocalOrParameter(node) ? SyntaxFactory.IdentifierName(GetUniqueName(node.Identifier.Text)) : node);
    }

    // The value of an override refers to the locals and parameters of its factory, which are renamed with the factory,
    // and keeps the names of types, namespaces and members, see #179.
    // A parameter of a lambda inside the value is declared there, so it keeps its name.
    private bool IsLocalOrParameter(IdentifierNameSyntax node)
    {
        if (_semanticModel is null || _semanticModel.SyntaxTree != node.SyntaxTree)
        {
            return false;
        }

        var symbolInfo = _semanticModel.GetSymbolInfo(node);
        return (symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault()) switch
        {
            ILocalSymbol => true,
            IParameterSymbol parameter => !parameter.DeclaringSyntaxReferences.Any(i => i.SyntaxTree == _root?.SyntaxTree && _root.Span.Contains(i.Span)),
            _ => false
        };
    }

    public override SyntaxToken VisitToken(SyntaxToken token)
    {
        if (Names.TryGetValue(token.Text, out var newName)
            && token.IsKind(SyntaxKind.IdentifierToken)
            && token.Parent is {} parent
            && (_semanticModel?.SyntaxTree != parent.SyntaxTree || IsRenamed(_semanticModel.GetSymbolInfo(parent).Symbol)))
        {
            token = SyntaxFactory.Identifier(newName).WithLeadingTrivia(token.LeadingTrivia).WithTrailingTrivia(token.TrailingTrivia);
        }

        return base.VisitToken(token);
    }

    // A parameter of a factory is renamed when the value of its override has already renamed it,
    // so the other references to the parameter are renamed too, see #183.
    private bool IsRenamed(ISymbol? symbol) =>
        symbol switch
        {
            ILocalSymbol => true,
            IParameterSymbol parameter => _renamedParameters.Contains(parameter),
            _ => false
        };

    private string GetUniqueName(string baseName) => Names.GetOrAdd(baseName, nameProvider.GetLocalUniqueVariableName);
}