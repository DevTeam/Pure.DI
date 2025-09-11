// ReSharper disable ConvertIfStatementToReturnStatement
// ReSharper disable InvertIf

namespace Pure.DI.Core.Code;

sealed class FactoryRewriter(
    IArguments arguments,
    ICompilations compilations,
    ISymbolNames symbolNames,
    FactoryRewriterContext ctx)
    : CSharpSyntaxRewriter, IFactoryRewriter
{
    private static readonly AttributeListSyntax MethodImplAttribute = SyntaxFactory.AttributeList().AddAttributes(
        SyntaxFactory.Attribute(
            SyntaxFactory.IdentifierName(Names.MethodImplAttributeName),
            SyntaxFactory.AttributeArgumentList().AddArguments(
                    SyntaxFactory.AttributeArgument(
                        SyntaxFactory.CastExpression(SyntaxFactory.ParseTypeName($"{Names.MethodImplOptionsName}"), SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(256)))))
                .WithTrailingTrivia(SyntaxTriviaList.Create(SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, " ")))));

    private static readonly IdentifierNameSyntax InjectionMarkerExpression = SyntaxFactory.IdentifierName(Names.InjectionMarker);
    private static readonly IdentifierNameSyntax InitializationMarkerExpression = SyntaxFactory.IdentifierName(Names.InitializationMarker);
    private static readonly IdentifierNameSyntax OverrideMarkerExpression = SyntaxFactory.IdentifierName(Names.OverrideMarker);
    private CodeContext? _ctx;
    private int _nestedBlockCounter;
    private int _nestedLambdaCounter;

    public bool IsFinishMarkRequired { get; private set; }

    public LambdaExpressionSyntax Rewrite(CodeContext codeCtx, LambdaExpressionSyntax lambda)
    {
        _ctx = codeCtx;
        return (LambdaExpressionSyntax)Visit(lambda);
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
        try
        {
            _nestedLambdaCounter++;
            var processedNode = base.VisitParenthesizedLambdaExpression(node);
            if (_nestedBlockCounter > 0
                || processedNode is not ParenthesizedLambdaExpressionSyntax lambda
                || compilations.GetLanguageVersion(ctx.Factory.Source.SemanticModel.Compilation) < LanguageVersion.CSharp10)
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
                        curStatement = CreateAssignmentExpression(returnBody)
                            .WithLeadingTrivia(statement.GetLeadingTrivia())
                            .WithTrailingTrivia(statement.GetTrailingTrivia());
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
            var prefix = GetPrefix(node);
            return SyntaxFactory.Block(
                    CreateAssignmentExpression(returnBody)
                        .WithLeadingTrivia(new SyntaxTriviaList().Add(SyntaxFactory.LineFeed).Add(prefix).Add(SyntaxFactory.Tab)),
                    SyntaxFactory.GotoStatement(
                        SyntaxKind.GotoStatement,
                        SyntaxFactory.IdentifierName(ctx.FinishLabel).WithLeadingTrivia(SyntaxFactory.Space))
                        .WithLeadingTrivia(new SyntaxTriviaList().Add(SyntaxFactory.LineFeed).Add(prefix).Add(SyntaxFactory.Tab))
                        .WithTrailingTrivia(new SyntaxTriviaList().Add(SyntaxFactory.LineFeed).Add(prefix))
                )
                .WithLeadingTrivia(prefix)
                .WithTrailingTrivia(SyntaxFactory.LineFeed);
        }

        return base.VisitReturnStatement(node);
    }

    private ExpressionStatementSyntax CreateAssignmentExpression(SyntaxNode returnBody) =>
        SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName(ctx.VarInjection.Var.Name).WithLeadingTrivia(SyntaxFactory.Space).WithTrailingTrivia(SyntaxFactory.Space),
                (ExpressionSyntax)Visit(returnBody).WithLeadingTrivia(SyntaxFactory.Space)));

    public override SyntaxNode VisitExpressionStatement(ExpressionStatementSyntax node)
    {
        node = (ExpressionStatementSyntax)base.VisitExpressionStatement(node)!;
        if (node.Expression is not InvocationExpressionSyntax
            {
                ArgumentList.Arguments.Count: > 0,
                Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax context } memberAccessExpression
            } invocation
            || context.Identifier.Text != ctx.Factory.Source.Context.Identifier.Text)
        {
            return node;
        }

        var name = memberAccessExpression.Name switch
        {
            GenericNameSyntax { TypeArgumentList.Arguments.Count: 1 } genericName => genericName.Identifier.Text,
            IdentifierNameSyntax identifierName => identifierName.Identifier.Text,
            _ => ""
        };

        ExpressionSyntax? expressionSyntax = null;
        var processed = name switch
        {
            nameof(IContext.Inject) => TryInject(invocation, out expressionSyntax),
            nameof(IContext.BuildUp) => TryInitialize(invocation, out expressionSyntax),
            nameof(IContext.Override) => TryOverride(invocation, out expressionSyntax),
            _ => false
        };

        if (!processed || expressionSyntax is null)
        {
            return node;
        }

        SyntaxNode newNode;
        if (node.Parent is null or BlockSyntax)
        {
            newNode = SyntaxFactory.ExpressionStatement(expressionSyntax)
                .WithLeadingTrivia(node.GetLeadingTrivia())
                .WithTrailingTrivia(node.GetTrailingTrivia());
        }
        else
        {
            var prefix = node.Ancestors().Any() ? node.Ancestors().First().GetLeadingTrivia() : new SyntaxTriviaList().Add(SyntaxFactory.LineFeed);
            newNode = SyntaxFactory.Block().AddStatements(
                    SyntaxFactory.ExpressionStatement(expressionSyntax)
                        .WithLeadingTrivia(new SyntaxTriviaList().Add(SyntaxFactory.LineFeed).AddRange(node.GetLeadingTrivia()))
                        .WithTrailingTrivia(new SyntaxTriviaList().Add(SyntaxFactory.LineFeed).AddRange(prefix)))
                .WithLeadingTrivia(prefix)
                .WithTrailingTrivia(node.GetTrailingTrivia());
        }

        return newNode;
    }

    private static SyntaxTrivia GetPrefix(SyntaxNode node)
    {
        var prefixes = node.GetLeadingTrivia()
            .Reverse()
            .Where(i => i.IsKind(SyntaxKind.WhitespaceTrivia))
            .Take(1)
            .ToList();

        var prefix = prefixes.Count > 0 ? prefixes.First() : SyntaxFactory.Tab;
        return prefix;
    }

    private bool TryInject(
        InvocationExpressionSyntax invocation,
        [NotNullWhen(true)] out ExpressionSyntax? expressionSyntax)
    {
        var value = invocation.ArgumentList.Arguments.Count switch
        {
            1 => invocation.ArgumentList.Arguments[0].Expression,
            2 => arguments.GetArgs(invocation.ArgumentList, "tag", "value").Last()?.Expression,
            _ => null
        };

        switch (value)
        {
            case IdentifierNameSyntax identifierName:
                ctx.Injections.Add(new Injection(identifierName.Identifier.Text, false));
            {
                expressionSyntax = InjectionMarkerExpression.WithLeadingTrivia(invocation.GetLeadingTrivia()).WithTrailingTrivia(invocation.GetTrailingTrivia());
                return true;
            }

            case DeclarationExpressionSyntax { Designation: SingleVariableDesignationSyntax singleVariableDesignationSyntax }:
                ctx.Injections.Add(new Injection(singleVariableDesignationSyntax.Identifier.Text, true));
            {
                expressionSyntax = InjectionMarkerExpression.WithLeadingTrivia(invocation.GetLeadingTrivia()).WithTrailingTrivia(invocation.GetTrailingTrivia());
                return true;
            }
        }

        expressionSyntax = null;
        return false;
    }

    private bool TryInitialize(
        InvocationExpressionSyntax invocation,
        [NotNullWhen(true)] out ExpressionSyntax? expressionSyntax)
    {
        var value = invocation.ArgumentList.Arguments.Count switch
        {
            1 => invocation.ArgumentList.Arguments[0].Expression,
            _ => null
        };

        switch (value)
        {
            case IdentifierNameSyntax identifierName:
                ctx.Initializers.Add(new Initializer(identifierName.Identifier.Text));
            {
                expressionSyntax = InitializationMarkerExpression.WithLeadingTrivia(invocation.GetLeadingTrivia()).WithTrailingTrivia(invocation.GetTrailingTrivia());
                return true;
            }

            case DeclarationExpressionSyntax { Designation: SingleVariableDesignationSyntax singleVariableDesignationSyntax }:
                ctx.Initializers.Add(new Initializer(singleVariableDesignationSyntax.Identifier.Text));
            {
                expressionSyntax = InitializationMarkerExpression.WithLeadingTrivia(invocation.GetLeadingTrivia()).WithTrailingTrivia(invocation.GetTrailingTrivia());
                return true;
            }
        }

        expressionSyntax = null;
        return false;
    }

    private static bool TryOverride(InvocationExpressionSyntax invocation, out ExpressionSyntax? expressionSyntax)
    {
        var value = invocation.ArgumentList.Arguments.Count > 0 ? invocation.ArgumentList.Arguments[0].Expression : null;
        if (value == null)
        {
            expressionSyntax = null;
            return false;
        }

        expressionSyntax = OverrideMarkerExpression.WithLeadingTrivia(invocation.GetLeadingTrivia()).WithTrailingTrivia(invocation.GetTrailingTrivia());
        return true;
    }

    public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        if (node.IsKind(SyntaxKind.SimpleMemberAccessExpression)
            && node is { Expression: IdentifierNameSyntax identifierName }
            && identifierName.Identifier.Text == ctx.Factory.Source.Context.Identifier.Text)
        {
            switch (node.Name.Identifier.Text)
            {
                case nameof(IContext.Tag):
                    return Visit(SyntaxFactory.ParseExpression($"{ctx.VarInjection.Injection.Tag.ValueToString()}"));

                case nameof(IContext.ConsumerTypes):
                    var consumers = _ctx?.Parents.Reverse().Select(i => $"typeof({symbolNames.GetGlobalName(i.Var.InstanceType)})").ToList() ?? [];
                    consumers.Add($"typeof({_ctx!.RootContext.Graph.Source.Name.FullName})");
                    return Visit(SyntaxFactory.ParseExpression($"new {Names.SystemNamespace}Type[{consumers.Count}]{{{string.Join(", ", consumers)}}}"));
            }
        }

        return base.VisitMemberAccessExpression(node);
    }

    internal record Injection(string VariableName, bool DeclarationRequired);

    internal record Initializer(string VariableName);
}