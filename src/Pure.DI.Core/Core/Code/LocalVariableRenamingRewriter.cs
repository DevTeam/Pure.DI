// ReSharper disable InvertIf
namespace Pure.DI.Core.Code;

using System.Collections.Generic;

internal class LocalVariableRenamingRewriter : CSharpSyntaxRewriter
{
    private readonly Dictionary<string, string> _identifierNames = new();
    private readonly IIdGenerator _idGenerator;
    private readonly SemanticModel _semanticModel;

    public LocalVariableRenamingRewriter(
        IIdGenerator idGenerator,
        SemanticModel semanticModel)
        : base(true)
    {
        _idGenerator = idGenerator;
        _semanticModel = semanticModel;
    }

    public SimpleLambdaExpressionSyntax Rewrite(SimpleLambdaExpressionSyntax lambda) => 
        (SimpleLambdaExpressionSyntax)VisitSimpleLambdaExpression(lambda)!;

    public override SyntaxNode? VisitVariableDeclarator(VariableDeclaratorSyntax node) => 
        base.VisitVariableDeclarator(node.WithIdentifier(SyntaxFactory.Identifier(GetNewName(node.Identifier.Text))));

    public override SyntaxNode? VisitSingleVariableDesignation(SingleVariableDesignationSyntax node) => 
        base.VisitSingleVariableDesignation(node.WithIdentifier(SyntaxFactory.Identifier(GetNewName(node.Identifier.Text))));

    public override SyntaxToken VisitToken(SyntaxToken token)
    {
        if (_identifierNames.TryGetValue(token.Text, out var newName)
            && token.IsKind(SyntaxKind.IdentifierToken)
            && token.Parent is { } parent
            && (_semanticModel.SyntaxTree != parent.SyntaxTree || _semanticModel.GetSymbolInfo(parent).Symbol is ILocalSymbol))
        {
            token = SyntaxFactory.Identifier(newName);
        }
        
        return base.VisitToken(token);
    }

    private string GetNewName(string name)
    {
        if (!_identifierNames.TryGetValue(name, out var newName))
        {
            newName = $"{Names.LocalVarName}{_idGenerator.Generate()}";
            _identifierNames.Add(name, newName);
        }

        return newName;
    }
}