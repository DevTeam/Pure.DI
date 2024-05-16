// ReSharper disable ConvertIfStatementToReturnStatement
// ReSharper disable InvertIf
namespace Pure.DI.Core.Code;

using System.Runtime.CompilerServices;

internal sealed class FactoryRewriter(
    IArguments arguments,
    DpFactory factory,
    Variable variable,
    string finishLabel,
    ICollection<FactoryRewriter.Injection> injections)
    : CSharpSyntaxRewriter
{
    private static readonly AttributeListSyntax MethodImplAttribute = SyntaxFactory.AttributeList().AddAttributes(
            SyntaxFactory.Attribute(
                SyntaxFactory.IdentifierName(Names.MethodImplAttributeName),
                SyntaxFactory.AttributeArgumentList().AddArguments(
                    SyntaxFactory.AttributeArgument(
                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.ParseTypeName(Names.MethodImplOptionsName), SyntaxFactory.IdentifierName(Names.MethodImplAggressiveInliningOptionsName))))))
        .WithTrailingTrivia(SyntaxTriviaList.Create(SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, " ")));
    
    private static readonly IdentifierNameSyntax InjectionMarkerExpression = SyntaxFactory.IdentifierName(Names.InjectionMarker);
    private int _nestedLambdaCounter;
    private int _nestedBlockCounter;

    public bool IsFinishMarkRequired { get; private set; }

    public LambdaExpressionSyntax Rewrite(LambdaExpressionSyntax lambda) => 
        (LambdaExpressionSyntax)Visit(lambda);

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
        try
        {
            _nestedLambdaCounter++;
            var processedNode = base.VisitParenthesizedLambdaExpression(node);
            if (_nestedBlockCounter > 0
                || processedNode is not ParenthesizedLambdaExpressionSyntax lambda
                || factory.Source.SemanticModel.Compilation.GetLanguageVersion() < LanguageVersion.CSharp10)
            {
                return processedNode;
            }

            return lambda.AddAttributeLists(MethodImplAttribute);
        }
        finally
        {
            _nestedLambdaCounter--;
        }
    }

    public override SyntaxNode? VisitBlock(BlockSyntax block)
    {
        _nestedBlockCounter++;
        try
        {
            if (_nestedLambdaCounter == 1 && _nestedBlockCounter == 1)
            {
                var statements = new List<StatementSyntax>(); 
                foreach (var statement in block.Statements)
                {
                    var curStatement = statement;
                    if (curStatement is ReturnStatementSyntax { Expression: {} returnBody })
                    {
                        curStatement = CreateAssignmentExpression(returnBody);
                    }
                    else
                    {
                        curStatement = (StatementSyntax)Visit(curStatement);
                    }
                    
                    statements.Add(curStatement);
                }

                return SyntaxFactory.Block(statements);
            }
            else
            {
                return base.VisitBlock(block);   
            }
        }
        finally
        {
            _nestedBlockCounter--;
        }
    }

    public override SyntaxNode? VisitReturnStatement(ReturnStatementSyntax node)
    {
        if (_nestedLambdaCounter == 1 && node.Expression is {} returnBody)
        {
            IsFinishMarkRequired = true;
            return SyntaxFactory.Block(
                CreateAssignmentExpression(returnBody),
                SyntaxFactory.GotoStatement(
                    SyntaxKind.GotoStatement,
                    SyntaxFactory.IdentifierName(finishLabel).WithLeadingTrivia(SyntaxFactory.Space)).WithLeadingTrivia(SyntaxFactory.Space).WithTrailingTrivia(SyntaxFactory.Space)
            ).WithLeadingTrivia(node.GetLeadingTrivia());
        }
        
        return base.VisitReturnStatement(node);
    }

    private ExpressionStatementSyntax CreateAssignmentExpression(SyntaxNode returnBody) =>
        SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName(variable.VariableName).WithLeadingTrivia(SyntaxFactory.Space).WithTrailingTrivia(SyntaxFactory.Space), 
                (ExpressionSyntax)Visit(returnBody).WithLeadingTrivia(SyntaxFactory.Space)));

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
                && ctx.Identifier.Text == factory.Source.Context.Identifier.Text
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
                && ctx2.Identifier.Text == factory.Source.Context.Identifier.Text
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
        var value = invocation.ArgumentList.Arguments.Count switch
        {
            1 => invocation.ArgumentList.Arguments[0].Expression,
            2 => arguments.GetArgs(invocation.ArgumentList, "tag", "value").Last()?.Expression,
            _ => default
        };

        switch (value)
        {
            case IdentifierNameSyntax identifierName:
                injections.Add(new Injection(identifierName.Identifier.Text, false));
            {
                visitInvocationExpression = InjectionMarkerExpression;
                return true;
            }

            case DeclarationExpressionSyntax { Designation: SingleVariableDesignationSyntax singleVariableDesignationSyntax }:
                injections.Add(new Injection(singleVariableDesignationSyntax.Identifier.Text, true));
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
            && identifierName.Identifier.Text == factory.Source.Context.Identifier.Text)
        {
            var token = SyntaxFactory.ParseToken(variable.Injection.Tag.ValueToString());
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
            
            if (token.IsKind(SyntaxKind.NullKeyword))
            {
                return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, token);
            }
        }
        
        return base.VisitMemberAccessExpression(node);
    }

    internal record Injection(string VariableName, bool DeclarationRequired);
}