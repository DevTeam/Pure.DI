// ReSharper disable ConvertIfStatementToReturnStatement
namespace Pure.DI.Core.CSharp;

internal class FactoryRewriter : CSharpSyntaxRewriter
{
    private static readonly IdentifierNameSyntax InjectionMarkerExpression = SyntaxFactory.IdentifierName(Variable.InjectionMarker);
    private readonly DpFactory _factory;
    private readonly Variable _variable;
    private readonly string _finishMark;
    private readonly ICollection<Injection> _injections;
    private int _nestedLambdaCounter;

    public FactoryRewriter(
        DpFactory factory,
        Variable variable,
        string finishMark,
        ICollection<Injection> injections)
    {
        _factory = factory;
        _variable = variable;
        _finishMark = finishMark;
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
                    SyntaxFactory.IdentifierName(_finishMark).WithLeadingTrivia(SyntaxFactory.Space)).WithLeadingTrivia(SyntaxFactory.Space).WithTrailingTrivia(SyntaxFactory.Space)
            ).WithLeadingTrivia(node.GetLeadingTrivia());
        }
        
        return base.VisitReturnStatement(node);
    }
    
    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax invocation)
    {
        if (invocation.ArgumentList.Arguments.Count > 0 
            && invocation.Expression is MemberAccessExpressionSyntax
            {
                Name: GenericNameSyntax
                {
                    Identifier.Text: nameof(IContext.Inject),
                    TypeArgumentList.Arguments: [not null]
                },
                Expression: IdentifierNameSyntax ctx
            }
            && ctx.Identifier.Text == _factory.Source.Context.Identifier.Text)
        {
            switch (invocation.ArgumentList.Arguments.Last().Expression)
            {
                case IdentifierNameSyntax identifierName:
                    _injections.Add(new Injection(identifierName.Identifier.Text, false));
                    return InjectionMarkerExpression;
                
                case DeclarationExpressionSyntax { Designation: SingleVariableDesignationSyntax singleVariableDesignationSyntax }:
                    _injections.Add(new Injection(singleVariableDesignationSyntax.Identifier.Text, true));
                    return InjectionMarkerExpression;
            }
        }

        return base.VisitInvocationExpression(invocation);
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

    internal record Injection(string VariableName, bool DeclarationRequired);
}