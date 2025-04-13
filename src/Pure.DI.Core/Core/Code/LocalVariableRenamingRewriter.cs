// ReSharper disable InvertIf

namespace Pure.DI.Core.Code;

sealed class LocalVariableRenamingRewriter(
    ITriviaTools triviaTools,
    IVariableNameProvider variableNameProvider)
    : CSharpSyntaxRewriter, ILocalVariableRenamingRewriter
{
    private Dictionary<string, string> Names { get; init; } = [];
    private bool _formatCode;
    private bool _isOverride;
    private bool _forcibleRename;
    private SemanticModel? _semanticModel;

    public SyntaxNode Rewrite(SemanticModel semanticModel, bool formatCode, bool isOverride, SyntaxNode lambda)
    {
        _semanticModel = semanticModel;
        _formatCode = formatCode;
        _isOverride = isOverride;
        _forcibleRename = _isOverride;
        return Visit(lambda);
    }

    public ILocalVariableRenamingRewriter Clone()
    {
        return new LocalVariableRenamingRewriter(triviaTools, variableNameProvider)
        {
            Names = new Dictionary<string, string>(Names)
        };
    }

    public override SyntaxNode? VisitVariableDeclarator(VariableDeclaratorSyntax node) =>
        base.VisitVariableDeclarator(node.WithIdentifier(SyntaxFactory.Identifier(GetUniqueName(node.Identifier.Text))));

    public override SyntaxNode? VisitSingleVariableDesignation(SingleVariableDesignationSyntax node) =>
        base.VisitSingleVariableDesignation(node.WithIdentifier(SyntaxFactory.Identifier(GetUniqueName(node.Identifier.Text))));

    public override SyntaxNode? VisitParameter(ParameterSyntax node) =>
        base.VisitParameter(
            Names.ContainsKey(node.Identifier.Text)
                ? node.WithIdentifier(SyntaxFactory.Identifier(GetUniqueName(node.Identifier.Text)))
                : node);

    public override SyntaxNode? VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
    {
        _forcibleRename = false;
        try
        {
            return base.VisitObjectCreationExpression(node);
        }
        finally
        {
            _forcibleRename = _isOverride;
        }
    }

    public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node) =>
        base.VisitIdentifierName(_forcibleRename ? SyntaxFactory.IdentifierName(GetUniqueName(node.Identifier.Text)) : node);

    public override SyntaxToken VisitToken(SyntaxToken token)
    {
        if (Names.TryGetValue(token.Text, out var newName)
            && token.IsKind(SyntaxKind.IdentifierToken)
            && token.Parent is {} parent
            && (_semanticModel?.SyntaxTree != parent.SyntaxTree || _semanticModel.GetSymbolInfo(parent).Symbol is ILocalSymbol))
        {
            token = triviaTools.PreserveTrivia(token, SyntaxFactory.Identifier(newName), _formatCode);
        }

        return base.VisitToken(token);
    }

    private string GetUniqueName(string baseName)
    {
        if (!Names.TryGetValue(baseName, out var newName))
        {
            newName = variableNameProvider.GetLocalUniqueVariableName(baseName);
            Names.Add(baseName, newName);
        }

        return newName;
    }
}