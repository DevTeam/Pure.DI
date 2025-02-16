// ReSharper disable InvertIf

namespace Pure.DI.Core.Code;

using System.Collections.Generic;

internal sealed class LocalVariableRenamingRewriter(
    ITriviaTools triviaTools,
    IVariableNameProvider variableNameProvider)
    : CSharpSyntaxRewriter, ILocalVariableRenamingRewriter
{
    private readonly Dictionary<string, string> _names = new();
    private BuildContext? _ctx;
    private SemanticModel? _semanticModel;

    public LambdaExpressionSyntax Rewrite(BuildContext ctx, LambdaExpressionSyntax lambda)
    {
        _semanticModel = ctx.DependencyGraph.Source.SemanticModel;
        _ctx = ctx;
        return (LambdaExpressionSyntax)Visit(lambda);
    }

    public override SyntaxNode? VisitVariableDeclarator(VariableDeclaratorSyntax node) =>
        base.VisitVariableDeclarator(node.WithIdentifier(SyntaxFactory.Identifier(GetUniqueName(node.Identifier.Text))));

    public override SyntaxNode? VisitSingleVariableDesignation(SingleVariableDesignationSyntax node) =>
        base.VisitSingleVariableDesignation(node.WithIdentifier(SyntaxFactory.Identifier(GetUniqueName(node.Identifier.Text))));

    public override SyntaxToken VisitToken(SyntaxToken token)
    {
        if (_names.TryGetValue(token.Text, out var newName)
            && token.IsKind(SyntaxKind.IdentifierToken)
            && token.Parent is { } parent
            && (_semanticModel?.SyntaxTree != parent.SyntaxTree || _semanticModel.GetSymbolInfo(parent).Symbol is ILocalSymbol))
        {
            token = triviaTools.PreserveTrivia(_ctx!.DependencyGraph.Source.Hints, SyntaxFactory.Identifier(newName), token);
        }

        return base.VisitToken(token);
    }

    private string GetUniqueName(string baseName)
    {
        if (!_names.TryGetValue(baseName, out var newName))
        {
            newName = variableNameProvider.GetLocalUniqueVariableName(baseName);
            _names.Add(baseName, newName);
        }

        return newName;
    }
}