// ReSharper disable ConvertIfStatementToReturnStatement
// ReSharper disable InvertIf

namespace Pure.DI.Core.Code;

sealed class FactoryRewriter(
    IArguments arguments,
    ICompilations compilations,
    DpFactory factory,
    VarInjection varInjection,
    string finishLabel,
    ICollection<FactoryRewriter.Injection> injections,
    ICollection<FactoryRewriter.Initializer> initializers,
    ITriviaTools triviaTools,
    ISymbolNames symbolNames)
    : CSharpSyntaxRewriter
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

    public LambdaExpressionSyntax Rewrite(CodeContext ctx, LambdaExpressionSyntax lambda)
    {
        _ctx = ctx;
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
                    if (curStatement is ReturnStatementSyntax { Expression: {} returnBody })
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
        if (_nestedLambdaCounter == 1 && node.Expression is {} returnBody)
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
        triviaTools.PreserveTrivia(owner, SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName(varInjection.Var.Name).WithLeadingTrivia(SyntaxFactory.Space).WithTrailingTrivia(SyntaxFactory.Space),
                (ExpressionSyntax)Visit(returnBody).WithLeadingTrivia(SyntaxFactory.Space))), _ctx!.RootContext.Graph.Source.Hints.IsFormatCodeEnabled);

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
            newNode = SyntaxFactory.ExpressionStatement(expressionSyntax).WithLeadingTrivia(SyntaxFactory.LineFeed).WithTrailingTrivia(SyntaxFactory.LineFeed);
        }
        else
        {
            newNode = SyntaxFactory.Block().AddStatements(SyntaxFactory.ExpressionStatement(expressionSyntax).WithLeadingTrivia(SyntaxFactory.LineFeed).WithTrailingTrivia(SyntaxFactory.LineFeed));
        }

        return triviaTools.PreserveTrivia(node, newNode, _ctx!.RootContext.Graph.Source.Hints.IsFormatCodeEnabled);
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
                injections.Add(new Injection(identifierName.Identifier.Text, false));
            {
                expressionSyntax = triviaTools.PreserveTrivia(invocation, InjectionMarkerExpression, _ctx!.RootContext.Graph.Source.Hints.IsFormatCodeEnabled);
                return true;
            }

            case DeclarationExpressionSyntax { Designation: SingleVariableDesignationSyntax singleVariableDesignationSyntax }:
                injections.Add(new Injection(singleVariableDesignationSyntax.Identifier.Text, true));
            {
                expressionSyntax = triviaTools.PreserveTrivia(invocation, InjectionMarkerExpression, _ctx!.RootContext.Graph.Source.Hints.IsFormatCodeEnabled);
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
                initializers.Add(new Initializer(identifierName.Identifier.Text));
            {
                expressionSyntax = triviaTools.PreserveTrivia(invocation, InitializationMarkerExpression, _ctx!.RootContext.Graph.Source.Hints.IsFormatCodeEnabled);
                return true;
            }

            case DeclarationExpressionSyntax { Designation: SingleVariableDesignationSyntax singleVariableDesignationSyntax }:
                initializers.Add(new Initializer(singleVariableDesignationSyntax.Identifier.Text));
            {
                expressionSyntax = triviaTools.PreserveTrivia(invocation, InitializationMarkerExpression, _ctx!.RootContext.Graph.Source.Hints.IsFormatCodeEnabled);
                return true;
            }
        }

        expressionSyntax = null;
        return false;
    }

    private bool TryOverride(InvocationExpressionSyntax invocation, out ExpressionSyntax? expressionSyntax)
    {
        var value = invocation.ArgumentList.Arguments.Count > 0 ? invocation.ArgumentList.Arguments[0].Expression : null;
        if (value == null)
        {
            expressionSyntax = null;
            return false;
        }

        expressionSyntax = triviaTools.PreserveTrivia(invocation, OverrideMarkerExpression, _ctx!.RootContext.Graph.Source.Hints.IsFormatCodeEnabled);
        return true;
    }

    public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        if (node.IsKind(SyntaxKind.SimpleMemberAccessExpression)
            && node is { Expression: IdentifierNameSyntax identifierName }
            && identifierName.Identifier.Text == factory.Source.Context.Identifier.Text)
        {
            switch (node.Name.Identifier.Text)
            {
                case nameof(IContext.Tag):
                    return Visit(SyntaxFactory.ParseExpression($" {varInjection.Injection.Tag.ValueToString()}"));

                case nameof(IContext.ConsumerTypes):
                    List<string> consumers;
                    // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                    if (_ctx!.RootContext.Graph.Graph.TryGetOutEdges(varInjection.Var.AbstractNode.Node, out var outDeps))
                    {
                        consumers = outDeps
                            .GroupBy(i => i.Target.Type, SymbolEqualityComparer.Default)
                            .Select(i => i.First().Target)
                            .OrderBy(i => i.Binding.Id)
                            .Select(target => $"typeof({symbolNames.GetGlobalName(target.Type)})")
                            .ToList();
                    }
                    else
                    {
                        consumers = [];
                    }

                    if (consumers.Count == 0)
                    {
                        consumers.Add($"typeof({_ctx!.RootContext.Graph.Source.Name.FullName})");
                    }

                    return Visit(SyntaxFactory.ParseExpression($" new {Names.SystemNamespace}Type[{consumers.Count}]{{{string.Join(", ", consumers)}}}"));
            }
        }

        return base.VisitMemberAccessExpression(node);
    }

    internal record Injection(string VariableName, bool DeclarationRequired);

    internal record Initializer(string VariableName);
}