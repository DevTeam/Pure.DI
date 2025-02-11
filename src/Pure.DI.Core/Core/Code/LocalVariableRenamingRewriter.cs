// ReSharper disable InvertIf

namespace Pure.DI.Core.Code;

using System.Collections.Generic;

internal sealed class LocalVariableRenamingRewriter(
    IIdGenerator idGenerator,
    SemanticModel semanticModel,
    ITriviaTools triviaTools)
    : CSharpSyntaxRewriter
{
    private readonly Dictionary<string, string> _identifierNames = new();
    private BuildContext? _ctx;

    public LambdaExpressionSyntax Rewrite(BuildContext ctx, LambdaExpressionSyntax lambda)
    {
        _ctx = ctx;
        return (LambdaExpressionSyntax)Visit(lambda);
    }

    public override SyntaxNode? VisitVariableDeclarator(VariableDeclaratorSyntax node) =>
        base.VisitVariableDeclarator(node.WithIdentifier(SyntaxFactory.Identifier(GetUniqueName(node.Identifier.Text))));

    public override SyntaxNode? VisitSingleVariableDesignation(SingleVariableDesignationSyntax node) =>
        base.VisitSingleVariableDesignation(node.WithIdentifier(SyntaxFactory.Identifier(GetUniqueName(node.Identifier.Text))));

    public override SyntaxToken VisitToken(SyntaxToken token)
    {
        if (_identifierNames.TryGetValue(token.Text, out var newName)
            && token.IsKind(SyntaxKind.IdentifierToken)
            && token.Parent is { } parent
            && (semanticModel.SyntaxTree != parent.SyntaxTree || semanticModel.GetSymbolInfo(parent).Symbol is ILocalSymbol))
        {
            token = triviaTools.PreserveTrivia(_ctx!.DependencyGraph.Source.Hints, SyntaxFactory.Identifier(newName), token);
        }

        return base.VisitToken(token);
    }

    private string GetUniqueName(string name)
    {
        if (!_identifierNames.TryGetValue(name, out var newName))
        {
            newName = $"{Names.LocalVariablePrefix}{name.ToTitleCase()}{Names.Salt}{idGenerator.Generate()}";
            _identifierNames.Add(name, newName);
        }

        return newName;
    }
}