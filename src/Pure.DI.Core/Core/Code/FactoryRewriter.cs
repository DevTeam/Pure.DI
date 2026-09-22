// ReSharper disable ConvertIfStatementToReturnStatement
// ReSharper disable InvertIf

// ReSharper disable UseCollectionExpression
namespace Pure.DI.Core.Code;

sealed class FactoryRewriter(
    IArguments arguments,
    ICompilations compilations,
    ITypeResolver typeResolver,
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
    private readonly IParameterSymbol? _contextSymbol = ctx.Factory.Source.Context.SyntaxTree == ctx.Factory.Source.SemanticModel.SyntaxTree
        ? ctx.Factory.Source.SemanticModel.GetDeclaredSymbol(ctx.Factory.Source.Context)
        : null;
    private LambdaExpressionSyntax? _rootLambda;
    private CodeContext? _ctx;
    private int _nestedBlockCounter;
    private int _nestedLambdaCounter;

    public bool IsFinishMarkRequired { get; private set; }

    public LambdaExpressionSyntax Rewrite(CodeContext codeCtx, LambdaExpressionSyntax lambda)
    {
        _ctx = codeCtx;
        _rootLambda = lambda;
        return (LambdaExpressionSyntax)Visit(lambda);
    }

    public override SyntaxNode? VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
    {
        _nestedLambdaCounter++;
        try
        {
            node = ReplaceByBlockLambdaIfNeeded(node);
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
            node = ReplaceByBlockLambdaIfNeeded(node);
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

    private T ReplaceByBlockLambdaIfNeeded<T>(T node) where T: LambdaExpressionSyntax
    {
        if (_nestedLambdaCounter > 1 && node.ExpressionBody is {} expressionBody && GetContextAction(expressionBody) is not null)
        {
            node = (T)node.WithExpressionBody(null).WithBlock(
                SyntaxFactory.Block(
                    SyntaxFactory.ExpressionStatement(expressionBody).WithLeadingTrivia(SyntaxFactory.LineFeed).WithTrailingTrivia(SyntaxFactory.LineFeed)
                )
            );
        }

        return node;
    }

    public override SyntaxNode? VisitBlock(BlockSyntax block)
    {
        _nestedBlockCounter++;
        try
        {
            if (_nestedLambdaCounter == 1 && _nestedBlockCounter == 1)
            {
                var statements = new List<StatementSyntax>(block.Statements.Count);
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

    private (InvocationExpressionSyntax invocation, string name)? GetContextAction(ExpressionSyntax expression)
    {
        if (expression is not InvocationExpressionSyntax
            {
                ArgumentList.Arguments.Count: > 0,
                Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax context } memberAccessExpression
            } invocation
            || !IsContextIdentifier(context))
        {
            return null;
        }

        return memberAccessExpression.Name switch
        {
            GenericNameSyntax { TypeArgumentList.Arguments.Count: 1 } genericName => (invocation, genericName.Identifier.Text),
            IdentifierNameSyntax identifierName => (invocation, identifierName.Identifier.Text),
            _ => null
        };
    }

    public override SyntaxNode VisitExpressionStatement(ExpressionStatementSyntax node)
    {
        node = (ExpressionStatementSyntax)base.VisitExpressionStatement(node)!;
        if (GetContextAction(node.Expression) is not var (invocation, name))
        {
            return node;
        }

        ExpressionSyntax? expressionSyntax = null;
        var processed = name switch
        {
            nameof(IContext.Inject) => TryInject(invocation, out expressionSyntax),
            nameof(IContext.BuildUp) => TryInitialize(invocation, out expressionSyntax),
            nameof(IContext.Override) or nameof(IContext.Let) => TryOverride(invocation, out expressionSyntax),
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
                .WithLeadingTrivia(new SyntaxTriviaList().Add(SyntaxFactory.LineFeed).AddRange(node.GetLeadingTrivia()))
                .WithTrailingTrivia(new SyntaxTriviaList().Add(SyntaxFactory.LineFeed).AddRange(node.GetTrailingTrivia()));
        }
        else
        {
            var firstAncestor = node.Ancestors().FirstOrDefault();
            var prefix = firstAncestor?.GetLeadingTrivia() ?? new SyntaxTriviaList().Add(SyntaxFactory.LineFeed);
            newNode = SyntaxFactory.Block().AddStatements(
                    SyntaxFactory.ExpressionStatement(expressionSyntax)
                        .WithLeadingTrivia(new SyntaxTriviaList().Add(SyntaxFactory.LineFeed).AddRange(node.GetLeadingTrivia()))
                        .WithTrailingTrivia(new SyntaxTriviaList().Add(SyntaxFactory.LineFeed).AddRange(prefix)))
                .WithLeadingTrivia(prefix)
                .WithTrailingTrivia(node.GetTrailingTrivia());
        }

        return newNode;
    }

    public override SyntaxNode? VisitSwitchSection(SwitchSectionSyntax node)
    {
        var visited = (SwitchSectionSyntax?)base.VisitSwitchSection(node);
        if (visited is null)
        {
            return null;
        }

        // When ctx.Inject (or similar context action) is used directly inside a switch
        // case without an explicit block, VisitExpressionStatement wraps the inject call
        // in a new Block to keep the local variable in scope. However, this leaves any
        // following statements (such as `return workerA;`) outside the new block, which
        // causes a compile error: the local variable is not visible outside the block.
        // To fix this, if any statement in the section became a Block, we merge all
        // statements into a single Block so they share the same scope.
        if (visited.Statements.Count > 1 && visited.Statements.Any(s => s is BlockSyntax))
        {
            var mergedStatements = new List<StatementSyntax>(visited.Statements.Count);
            foreach (var statement in visited.Statements)
            {
                if (statement is BlockSyntax innerBlock)
                {
                    mergedStatements.AddRange(innerBlock.Statements);
                }
                else
                {
                    mergedStatements.Add(statement);
                }
            }

            var firstStatement = visited.Statements[0];
            var lastStatement = visited.Statements[visited.Statements.Count - 1];
            var mergedBlock = SyntaxFactory.Block(mergedStatements)
                .WithLeadingTrivia(firstStatement.GetLeadingTrivia())
                .WithTrailingTrivia(lastStatement.GetTrailingTrivia());

            return visited.WithStatements(new SyntaxList<StatementSyntax>(mergedBlock));
        }

        return visited;
    }

    private static SyntaxTrivia GetPrefix(SyntaxNode node)
    {
        var leadingTrivia = node.GetLeadingTrivia();
        for (var i = leadingTrivia.Count - 1; i >= 0; i--)
        {
            var trivia = leadingTrivia[i];
            if (trivia.IsKind(SyntaxKind.WhitespaceTrivia))
            {
                return trivia;
            }
        }

        return SyntaxFactory.Tab;
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

            case DeclarationExpressionSyntax { Designation: DiscardDesignationSyntax }:
                ctx.Injections.Add(new Injection("_", false));
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

    private bool IsContextIdentifier(IdentifierNameSyntax identifierName)
    {
        if (identifierName.Identifier.Text != ctx.Factory.Source.Context.Identifier.Text)
        {
            return false;
        }

        var semanticModel = ctx.Factory.Source.SemanticModel;
        var nearestLocalFunction = identifierName
            .Ancestors()
            .OfType<LocalFunctionStatementSyntax>()
            .FirstOrDefault(localFunction => localFunction.ParameterList.Parameters.Any(parameter => parameter.Identifier.Text == identifierName.Identifier.Text));

        if (nearestLocalFunction is not null)
        {
            return false;
        }

        var nearestLambda = identifierName
            .AncestorsAndSelf()
            .OfType<LambdaExpressionSyntax>()
            .FirstOrDefault(lambda => LambdaDeclaresParameter(lambda, identifierName.Identifier.Text));

        if (nearestLambda is not null && !ReferenceEquals(nearestLambda, _rootLambda))
        {
            return false;
        }

        if (_contextSymbol is not null && identifierName.SyntaxTree == semanticModel.SyntaxTree)
        {
            var symbol = semanticModel.GetSymbolInfo(identifierName).Symbol;
            return symbol is not null && SymbolEqualityComparer.Default.Equals(symbol, _contextSymbol);
        }

        if (_rootLambda is null)
        {
            return false;
        }

        return nearestLambda is null || ReferenceEquals(nearestLambda, _rootLambda);
    }

    private static bool LambdaDeclaresParameter(LambdaExpressionSyntax lambda, string name) =>
        lambda switch
        {
            SimpleLambdaExpressionSyntax { Parameter.Identifier.Text: var parameterName } => parameterName == name,
            ParenthesizedLambdaExpressionSyntax parenthesized => parenthesized.ParameterList.Parameters.Any(parameter => parameter.Identifier.Text == name),
            _ => false
        };

    public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        if (node.IsKind(SyntaxKind.SimpleMemberAccessExpression)
            && node is { Expression: IdentifierNameSyntax identifierName }
            && IsContextIdentifier(identifierName))
        {
            switch (node.Name.Identifier.Text)
            {
                case nameof(IContext.Tag):
                    return SyntaxFactory.ParseExpression($"{ctx.VarInjection.Injection.Tag.ValueToString()}");

                case nameof(IContext.ConsumerTypes):
                    var consumers = GetConsumers(_ctx!.RootContext.Graph.Source).ToList();
                    return SyntaxFactory.ParseExpression($"new {Names.SystemNamespace}Type[{consumers.Count}]{{{string.Join(", ", consumers)}}}");

                case nameof(IContext.ConsumerType):
                    var consumer = GetConsumers(_ctx!.RootContext.Graph.Source).First();
                    return SyntaxFactory.ParseExpression(consumer);

                case nameof(IContext.Lock):
                    if (_ctx is {} codeCtx)
                    {
                        codeCtx.RootContext.LockIsInUse = true;
                    }

                    var lockFieldName = _ctx?.RootContext.Root.IsStatic == true
                        ? Names.PerResolveLockFieldName
                        : Names.LockFieldName;
                    return SyntaxFactory.IdentifierName(lockFieldName);

                case nameof(IContext.DependencyCount):
                    var dependencyCount = _ctx!.RootContext.Graph.Graph.TryGetInEdges(_ctx.VarInjection.Var.AbstractNode.Node, out var edges)
                        ? edges.Count
                        : 0;
                    return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(dependencyCount));

                case nameof(IContext.IsLockRequired):
                    return _ctx?.IsLockRequired == true
                        ? SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression,  SyntaxFactory.Token(SyntaxKind.TrueKeyword))
                        : SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression, SyntaxFactory.Token(SyntaxKind.FalseKeyword));

                case nameof(IContext.RootType):
                    {
                        // #154 When a user root's per-block lambda body is generated inside a wrapping lightweight
                        // root (LightweightKind.RootsProvider), the surrounding RootContext points to the wrapping
                        // instance and ctx.RootType would otherwise be the lightweight root type. The user expects
                        // ctx.RootType to match the type passed to Resolve<T>(), so we look up the actual user root
                        // (any non-wrapper root whose Injection.Type matches a node in the Parents chain or the
                        // current variable).
                        var rootContext = _ctx!.RootContext;
                        var rootContextRootType = rootContext.Root.Injection.Type;
                        ITypeSymbol? owningRootType = null;
                        if (rootContext.Root.Source.LightweightKind == LightweightKind.RootsProvider)
                        {
                            foreach (var candidate in rootContext.Graph.Roots)
                            {
                                if (candidate.Source.LightweightKind == LightweightKind.RootsProvider)
                                {
                                    continue;
                                }

                                var candidateType = candidate.Injection.Type;
                                var found = false;
                                foreach (var parent in _ctx.Parents)
                                {
                                    if (SymbolEqualityComparer.Default.Equals(parent.Var.AbstractNode.Node.Type, candidateType))
                                    {
                                        found = true;
                                        break;
                                    }
                                }

                                if (!found && SymbolEqualityComparer.Default.Equals(_ctx.VarInjection.Var.AbstractNode.Node.Type, candidateType))
                                {
                                    found = true;
                                }

                                if (found)
                                {
                                    owningRootType = candidateType;
                                    break;
                                }
                            }
                        }

                        var rootType = owningRootType ?? rootContextRootType;
                        return SyntaxFactory.ParseExpression($"typeof({typeResolver.ResolveRuntime(rootContext.Graph.Source, rootType)})");
                    }

                case nameof(IContext.RootName):
                    return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(_ctx!.RootContext.Root.DisplayName));
            }
        }

        return base.VisitMemberAccessExpression(node);
    }

    internal record Injection(string VariableName, bool DeclarationRequired);

    internal record Initializer(string VariableName);

    private IEnumerable<string> GetConsumers(MdSetup setup)
    {
        foreach (var parent in _ctx?.Parents.Reverse() ?? [])
        {
            yield return $"typeof({typeResolver.ResolveRuntime(setup, parent.Var.InstanceType)})";
        }

        yield return $"typeof({_ctx!.RootContext.Graph.Source.Name.FullName})";
    }
}
