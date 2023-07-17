// ReSharper disable ConvertIfStatementToReturnStatement
// ReSharper disable InvertIf
namespace Pure.DI.Core.CSharp;

internal class FactoryRewriter : CSharpSyntaxRewriter
{
    private static readonly IdentifierNameSyntax InjectionMarkerExpression = SyntaxFactory.IdentifierName(Variable.InjectionMarker);
    private readonly DpFactory _factory;
    private readonly Variable _variable;
    private readonly object? _contextTag;
    private readonly string _finishLabel;
    private readonly ICollection<Injection> _injections;
    private int _nestedLambdaCounter;

    public FactoryRewriter(
        DpFactory factory,
        Variable variable,
        object? contextTag,
        string finishLabel,
        ICollection<Injection> injections)
    {
        _factory = factory;
        _variable = variable;
        _contextTag = contextTag;
        _finishLabel = finishLabel;
        _injections = injections;
    }
    
    public bool IsFinishMarkRequired { get; private set; }

    public SimpleLambdaExpressionSyntax Rewrite(SimpleLambdaExpressionSyntax lambda)
    {
        return (SimpleLambdaExpressionSyntax)VisitSimpleLambdaExpression(lambda)!;
    }

    public override SyntaxNode? VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
    {
        _nestedLambdaCounter++;
        try
        {
            return base.VisitSimpleLambdaExpression(node);
        }
        finally
        {
            _nestedLambdaCounter--;
        }
    }

    public override SyntaxNode? VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
    {
        _nestedLambdaCounter++;
        try
        {
            return base.VisitParenthesizedLambdaExpression(node);
        }
        finally
        {
            _nestedLambdaCounter--;   
        }
    }
    
    public override SyntaxNode? VisitReturnStatement(ReturnStatementSyntax node)
    {
        if (_nestedLambdaCounter == 1 && node.Expression is {} returnBody)
        {
            IsFinishMarkRequired = true;
            return SyntaxFactory.Block(
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName(_variable.Name).WithLeadingTrivia(SyntaxFactory.Space).WithTrailingTrivia(SyntaxFactory.Space), 
                        (ExpressionSyntax)Visit(returnBody).WithLeadingTrivia(SyntaxFactory.Space))),
                SyntaxFactory.GotoStatement(
                    SyntaxKind.GotoStatement,
                    SyntaxFactory.IdentifierName(_finishLabel).WithLeadingTrivia(SyntaxFactory.Space)).WithLeadingTrivia(SyntaxFactory.Space).WithTrailingTrivia(SyntaxFactory.Space)
            ).WithLeadingTrivia(node.GetLeadingTrivia());
        }
        
        return base.VisitReturnStatement(node);
    }
    
    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax invocation)
    {
        if (invocation.ArgumentList.Arguments.Count > 0)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax
                {
                    Name: GenericNameSyntax
                    {
                        Identifier.Text: nameof(IContext.Inject),
                        TypeArgumentList.Arguments: [not null]
                    },
                    Expression: IdentifierNameSyntax ctx
                }
                && ctx.Identifier.Text == _factory.Source.Context.Identifier.Text
                && TryInject(invocation, out var visitInvocationExpression))
            {
                return visitInvocationExpression;
            }
            
            if (invocation.Expression is MemberAccessExpressionSyntax
                {
                    Name: IdentifierNameSyntax
                    {
                        Identifier.Text: nameof(IContext.Inject)
                    },
                    Expression: IdentifierNameSyntax ctx2
                }
                && ctx2.Identifier.Text == _factory.Source.Context.Identifier.Text
                && TryInject(invocation, out visitInvocationExpression))
            {
                return visitInvocationExpression;
            }
        }

        return base.VisitInvocationExpression(invocation);
    }

    private bool TryInject(
        InvocationExpressionSyntax invocation,
        [NotNullWhen(true)] out SyntaxNode? visitInvocationExpression)
    {
        switch (invocation.ArgumentList.Arguments.Last().Expression)
        {
            case IdentifierNameSyntax identifierName:
                _injections.Add(new Injection(identifierName.Identifier.Text, false));
            {
                visitInvocationExpression = InjectionMarkerExpression;
                return true;
            }

            case DeclarationExpressionSyntax { Designation: SingleVariableDesignationSyntax singleVariableDesignationSyntax }:
                _injections.Add(new Injection(singleVariableDesignationSyntax.Identifier.Text, true));
            {
                visitInvocationExpression = InjectionMarkerExpression;
                return true;
            }
        }

        visitInvocationExpression = default;
        return false;
    }

    public override SyntaxNode VisitExpressionStatement(ExpressionStatementSyntax node)
    {
        var newNode = (ExpressionStatementSyntax)base.VisitExpressionStatement(node)!;
        if (newNode.Expression.IsEquivalentTo(InjectionMarkerExpression))
        {
            return newNode
                .WithoutLeadingTrivia()
                .WithLeadingTrivia(SyntaxFactory.LineFeed)
                .WithTrailingTrivia(SyntaxFactory.LineFeed);
        }

        return newNode;
    }

    public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        if (node.IsKind(SyntaxKind.SimpleMemberAccessExpression)
            && node is { Expression: IdentifierNameSyntax identifierName, Name.Identifier.Text: nameof(IContext.Tag) } 
            && identifierName.Identifier.Text == _factory.Source.Context.Identifier.Text)
        {
            var token = SyntaxFactory.ParseToken(_contextTag.ValueToString());
            if (token.IsKind(SyntaxKind.NumericLiteralToken))
            {
                return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, token);
            }
            
            if (token.IsKind(SyntaxKind.CharacterLiteralToken))
            {
                return SyntaxFactory.LiteralExpression(SyntaxKind.CharacterLiteralExpression, token);
            }
            
            if (token.IsKind(SyntaxKind.StringLiteralToken))
            {
                return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, token);
            }
        }
        
        return base.VisitMemberAccessExpression(node);
    }

    internal record Injection(string VariableName, bool DeclarationRequired);
}