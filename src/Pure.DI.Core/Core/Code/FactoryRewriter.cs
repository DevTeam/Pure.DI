// ReSharper disable ConvertIfStatementToReturnStatement
// ReSharper disable InvertIf

namespace Pure.DI.Core.Code;

internal sealed class FactoryRewriter(
    IArguments arguments,
    ICompilations compilations,
    DpFactory factory,
    Variable variable,
    string finishLabel,
    ICollection<FactoryRewriter.Injection> injections,
    ICollection<FactoryRewriter.Initializer> initializers,
    ITriviaTools triviaTools)
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
    private static readonly IdentifierNameSyntax InitializationMarkerExpression = SyntaxFactory.IdentifierName(Names.InitializationMarker);
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
                || compilations.GetLanguageVersion(factory.Source.SemanticModel.Compilation) < LanguageVersion.CSharp10)
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
                    if (curStatement is ReturnStatementSyntax { Expression: { } returnBody })
                    {
                        curStatement = CreateAssignmentExpression(returnBody, statement);
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
        if (_nestedLambdaCounter == 1 && node.Expression is { } returnBody)
        {
            IsFinishMarkRequired = true;
            return SyntaxFactory.Block(
                CreateAssignmentExpression(returnBody, node),
                SyntaxFactory.GotoStatement(
                    SyntaxKind.GotoStatement,
                    SyntaxFactory.IdentifierName(finishLabel).WithLeadingTrivia(SyntaxFactory.Space)).WithLeadingTrivia(SyntaxFactory.Space).WithTrailingTrivia(SyntaxFactory.Space)
            )
                .WithLeadingTrivia(node.GetLeadingTrivia())
                .WithTrailingTrivia(node.GetTrailingTrivia());
        }

        return base.VisitReturnStatement(node);
    }

    private ExpressionStatementSyntax CreateAssignmentExpression(SyntaxNode returnBody, StatementSyntax owner) =>
        triviaTools.PreserveTrivia(
            SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName(variable.VariableName).WithLeadingTrivia(SyntaxFactory.Space).WithTrailingTrivia(SyntaxFactory.Space),
                    (ExpressionSyntax)Visit(returnBody).WithLeadingTrivia(SyntaxFactory.Space))),
            owner);
    
    public override SyntaxNode VisitExpressionStatement(ExpressionStatementSyntax node)
    {
        node = (ExpressionStatementSyntax)base.VisitExpressionStatement(node)!;
        if (node.Expression is not InvocationExpressionSyntax
            {
                ArgumentList.Arguments.Count: > 0,
                Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax ctx } memberAccessExpression
            } invocation
            || ctx.Identifier.Text != factory.Source.Context.Identifier.Text)
        {
            return node;
        }

        var name = memberAccessExpression.Name switch
        {
            GenericNameSyntax { TypeArgumentList.Arguments.Count: 1 } genericName => genericName.Identifier.Text,
            IdentifierNameSyntax identifierName => identifierName.Identifier.Text,
            _ => ""
        };

        ExpressionSyntax? expressionSyntax = default;
        var processed = name switch
        {
            nameof(IContext.Inject) => TryInject(invocation, out expressionSyntax),
            nameof(IContext.BuildUp) => TryInitialize(invocation, out expressionSyntax),
            _ => false
        };

        if (!processed || expressionSyntax is null)
        {
            return node;
        }

        SyntaxNode newNode;
        if (node.Parent is null or BlockSyntax)
        {
            newNode = SyntaxFactory.ExpressionStatement(expressionSyntax).WithLeadingTrivia(SyntaxFactory.LineFeed).WithTrailingTrivia(SyntaxFactory.LineFeed);
        }
        else
        {
            newNode = SyntaxFactory.Block().AddStatements(SyntaxFactory.ExpressionStatement(expressionSyntax).WithLeadingTrivia(SyntaxFactory.LineFeed).WithTrailingTrivia(SyntaxFactory.LineFeed));
        }

        return triviaTools.PreserveTrivia(newNode, node);
    }

    private bool TryInject(
        InvocationExpressionSyntax invocation,
        [NotNullWhen(true)] out ExpressionSyntax? expressionSyntax)
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
                expressionSyntax = triviaTools.PreserveTrivia(InjectionMarkerExpression, invocation);
                return true;
            }

            case DeclarationExpressionSyntax { Designation: SingleVariableDesignationSyntax singleVariableDesignationSyntax }:
                injections.Add(new Injection(singleVariableDesignationSyntax.Identifier.Text, true));
            {
                expressionSyntax = triviaTools.PreserveTrivia(InjectionMarkerExpression, invocation);
                return true;
            }
        }

        expressionSyntax = default;
        return false;
    }
    
    private bool TryInitialize(
        InvocationExpressionSyntax invocation,
        [NotNullWhen(true)] out ExpressionSyntax? expressionSyntax)
    {
        var value = invocation.ArgumentList.Arguments.Count switch
        {
            1 => invocation.ArgumentList.Arguments[0].Expression,
            _ => default
        };

        switch (value)
        {
            case IdentifierNameSyntax identifierName:
                initializers.Add(new Initializer(identifierName.Identifier.Text));
            {
                expressionSyntax = triviaTools.PreserveTrivia(InitializationMarkerExpression, invocation);
                return true;
            }

            case DeclarationExpressionSyntax { Designation: SingleVariableDesignationSyntax singleVariableDesignationSyntax }:
                initializers.Add(new Initializer(singleVariableDesignationSyntax.Identifier.Text));
            {
                expressionSyntax = triviaTools.PreserveTrivia(InitializationMarkerExpression, invocation);
                return true;
            }
        }

        expressionSyntax = default;
        return false;
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
    
    internal record Initializer(string VariableName);
}