// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable MergeIntoPattern

namespace Pure.DI.Core.Code;

sealed class FactoryCodeBuilder(
    INodeInfo nodeInfo,
    IArguments arguments,
    ITypeResolver typeResolver,
    ICompilations compilations,
    ITriviaTools triviaTools,
    ISymbolNames symbolNames,
    IVariableNameProvider variableNameProvider,
    Func<IFactoryValidator> factoryValidatorFactory,
    Func<IInitializersWalker> initializersWalkerFactory,
    IOverridesRegistry overridesRegistry,
    ILocationProvider locationProvider)
    : ICodeBuilder<DpFactory>
{
    public const string DefaultInstanceValueName = "instance_1182D127";
    public static readonly ParenthesizedLambdaExpressionSyntax DefaultBindAttrParenthesizedLambda = SyntaxFactory.ParenthesizedLambdaExpression();
    public static readonly ParameterSyntax DefaultCtxParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("ctx_1182D127"));
    private static readonly string InjectionStatement = $"{Names.InjectionMarker};";
    private static readonly string InitializationStatement = $"{Names.InitializationMarker};";
    private static readonly string OverrideStatement = $"{Names.OverrideMarker};";

    public void Build(BuildContext ctx, in DpFactory factory)
    {
        var variable = ctx.Variable;
        var code = ctx.Code;
        var level = ctx.Level;
        var lockIsRequired = ctx.LockIsRequired;
        if (nodeInfo.IsLazy(variable.Node))
        {
            level++;
            lockIsRequired = null;
        }

        var originalLambda = factory.Source.Factory;
        // Simple factory
        if (factory.Source.IsSimpleFactory)
        {
            var block = new List<StatementSyntax>();
            foreach (var resolver in factory.Source.Resolvers)
            {
                if (resolver.ArgumentType is not {} argumentType || resolver.Parameter is not {} parameter)
                {
                    continue;
                }

                var valueDeclaration = SyntaxFactory.DeclarationExpression(
                    argumentType,
                    SyntaxFactory.SingleVariableDesignation(parameter.Identifier));

                var valueArg =
                    SyntaxFactory.Argument(valueDeclaration)
                        .WithRefOrOutKeyword(SyntaxFactory.Token(SyntaxKind.OutKeyword));

                var injection = SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(DefaultCtxParameter.Identifier),
                            SyntaxFactory.IdentifierName(nameof(IContext.Inject))))
                    .AddArgumentListArguments(valueArg);

                block.Add(SyntaxFactory.ExpressionStatement(injection));
            }

            if (factory.Source.MemberResolver is {} memberResolver
                && memberResolver.Member is {} member
                && memberResolver.TypeConstructor is {} typeConstructor)
            {
                ExpressionSyntax? value = null;
                var type = memberResolver.ContractType;
                ExpressionSyntax instance = member.IsStatic
                    ? SyntaxFactory.ParseTypeName(symbolNames.GetGlobalName(type))
                    : SyntaxFactory.IdentifierName(DefaultInstanceValueName);

                switch (member)
                {
                    case IFieldSymbol:
                    case IPropertySymbol:
                        value = SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            instance,
                            SyntaxFactory.IdentifierName(member.Name));
                        break;

                    case IMethodSymbol methodSymbol:
                        var args = methodSymbol.Parameters
                            .Select(i => SyntaxFactory.Argument(SyntaxFactory.IdentifierName(i.Name)))
                            .ToArray();

                        if (methodSymbol.IsGenericMethod)
                        {
                            var setup = variable.Setup;
                            var binding = variable.Node.Binding;
                            var typeArgs = new List<TypeSyntax>();
                            // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
                            foreach (var typeArg in methodSymbol.TypeArguments)
                            {
                                var argType = typeConstructor.ConstructReversed(typeArg);
                                if (binding.TypeConstructor is {} bindingTypeConstructor)
                                {
                                    argType = bindingTypeConstructor.Construct(setup, argType);
                                }

                                var typeName = symbolNames.GetGlobalName(argType);
                                typeArgs.Add(SyntaxFactory.ParseTypeName(typeName));
                            }

                            value = SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                instance,
                                SyntaxFactory.GenericName(member.Name).AddTypeArgumentListArguments(typeArgs.ToArray()));
                        }
                        else
                        {
                            value = SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                instance,
                                SyntaxFactory.IdentifierName(member.Name));
                        }

                        value = SyntaxFactory
                            .InvocationExpression(value)
                            .AddArgumentListArguments(args);

                        break;
                }

                if (value is not null)
                {
                    block.Add(SyntaxFactory.ReturnStatement(value));
                }
            }
            else
            {
                if (originalLambda.Block is {} lambdaBlock)
                {
                    block.AddRange(lambdaBlock.Statements);
                }
                else
                {
                    if (originalLambda.ExpressionBody is {} body)
                    {
                        block.Add(SyntaxFactory.ReturnStatement(body));
                    }
                }
            }

            originalLambda = SyntaxFactory.SimpleLambdaExpression(DefaultCtxParameter)
                .WithBlock(SyntaxFactory.Block(block));
        }

        // Rewrites syntax tree
        var finishLabel = $"{variable.VariableDeclarationName}Finish";
        var factoryExpression = (LambdaExpressionSyntax)factory.Source.LocalVariableRenamingRewriter.Clone().Rewrite(ctx.DependencyGraph.Source.SemanticModel, ctx.DependencyGraph.Source.Hints.IsFormatCodeEnabled, false, originalLambda);
        var injections = new List<FactoryRewriter.Injection>();
        var inits = new List<FactoryRewriter.Initializer>();
        var factoryRewriter = new FactoryRewriter(arguments, compilations, factory, variable, finishLabel, injections, inits, triviaTools, symbolNames);
        var lambda = factoryRewriter.Rewrite(ctx, factoryExpression);
        factoryValidatorFactory().Initialize(factory).Visit(lambda);
        SyntaxNode syntaxNode = lambda.Block is not null ? lambda.Block : SyntaxFactory.ExpressionStatement((ExpressionSyntax)lambda.Body);
        var lines = new List<TextLine>();
        if (!variable.IsDeclared && variable.HasCycledReference)
        {
            ctx.Code.AppendLine($"var {variable.VariableName} = default({ctx.BuildTools.GetDeclaration(variable, "")});");
            variable.IsDeclared = true;
        }

        if (syntaxNode is BlockSyntax curBlock)
        {
            if (!variable.IsDeclared)
            {
                code.AppendLine($"{ctx.BuildTools.GetDeclaration(variable)}{variable.VariableName};");
            }

            foreach (var statement in curBlock.Statements)
            {
                var text = statement.GetText();
                lines.AddRange(text.Lines);
            }
        }
        else
        {
            var leadingTrivia = syntaxNode.GetLeadingTrivia().ToFullString().Trim();
            if (!string.IsNullOrEmpty(leadingTrivia))
            {
                code.AppendLine(leadingTrivia);
            }

            code.Append($"{ctx.BuildTools.GetDeclaration(variable)}{variable.VariableName} = ");
            var text = syntaxNode.WithoutTrivia().GetText();
            lines.AddRange(text.Lines);
        }

        var injectionArgs = variable.Args.Where(i => i.Current.Injection.Kind is InjectionKind.FactoryInjection).ToList();
        var initializationArgs = variable.Args.Where(i => i.Current.Injection.Kind != InjectionKind.FactoryInjection).ToList();

        // Replaces injection markers by injection code
        if (injectionArgs.Count != injections.Count)
        {
            throw new CompileErrorException(
                string.Format(Strings.Error_Template_LifetimeDoesNotSupportCyclicDependencies, variable.Node.Lifetime),
                locationProvider.GetLocation(factory.Source.Source),
                LogId.ErrorInvalidMetadata);
        }

        if (factory.Initializers.Length != inits.Count)
        {
            throw new CompileErrorException(
                Strings.Error_InvalidNumberOfInitializers,
                locationProvider.GetLocation(factory.Source.Source),
                LogId.ErrorInvalidMetadata);
        }

        using var resolvers = injections
            .Zip(injectionArgs, (injection, argument) => (injection, argument))
            .Zip(factory.Resolvers, (i, resolver) => (i.injection, i.argument, resolver))
            .GetEnumerator();

        using var initializers = inits
            .Zip(factory.Initializers, (initialization, initializer) => (initialization, initializer))
            .GetEnumerator();

        var hasOverrides = factory.HasOverrides;
        var initializationArgsEnum = initializationArgs.Select(i => i.Current).GetEnumerator();
        ctx = ctx with { LockIsRequired = lockIsRequired, Level = level, AvoidLocalFunction = hasOverrides };
        var injectionsCtx = ctx;
        if (variable.IsLazy && variable.Node.Accumulators.Count > 0)
        {
            injectionsCtx = injectionsCtx with
            {
                Accumulators = injectionsCtx.Accumulators
                    .AsEnumerable()
                    .Select(i => i with { IsDeclared = i.IsRoot && i.IsDeclared })
                    .ToImmutableArray()
            };
        }

        var prefixes = new Stack<string>();
        foreach (var textLine in lines)
        {
            var line = textLine.ToString();
            var prefix = new string(line.TakeWhile(char.IsWhiteSpace).ToArray());
            if (prefix.Length > 0)
            {
                if (prefixes.Count == 0 || prefix.Length > prefixes.Peek().Length)
                {
                    prefixes.Push(prefix);
                }
                else
                {
                    if (prefixes.Count > 0 && prefix.Length < prefixes.Peek().Length)
                    {
                        prefixes.Pop();
                    }
                }
            }

            if (prefix.Length > 0 && prefixes.Count > 0 && line.StartsWith(prefix))
            {
                line = Formatting.IndentPrefix(new Indent(prefixes.Count - 1)) + line[prefix.Length..];
            }

            if (line.Contains(InjectionStatement) && resolvers.MoveNext())
            {
                // When an injection marker
                var (injection, argument, resolver) = resolvers.Current;
                var indent = prefixes.Count;
                using (code.Indent(indent))
                {
                    BuildOverrides(ctx, factory, resolver.Overrides, code);
                    if (hasOverrides)
                    {
                        foreach (var argStatement in GetArgsStatements(argument))
                        {
                            ctx.StatementBuilder.Build(ctx with { Variable = argStatement.Current }, argStatement);
                        }
                    }

                    ctx.StatementBuilder.Build(injectionsCtx with { Variable = argument.Current }, argument);
                    code.AppendLine($"{(injection.DeclarationRequired ? $"{typeResolver.Resolve(ctx.DependencyGraph.Source, argument.Current.Injection.Type)} " : "")}{injection.VariableName} = {ctx.BuildTools.OnInjected(ctx, argument.Current)};");
                }

                continue;
            }

            if (line.Contains(InitializationStatement) && initializers.MoveNext())
            {
                var (initialization, initializer) = initializers.Current;
                BuildOverrides(ctx, factory, initializer.Overrides, code);
                if (hasOverrides)
                {
                    foreach (var argument in initializationArgs)
                    foreach (var argStatement in GetArgsStatements(argument))
                    {
                        ctx.StatementBuilder.Build(ctx with { Variable = argStatement.Current }, argStatement);
                    }
                }

                var initializersWalker = initializersWalkerFactory().Ininitialize(initialization.VariableName, initializationArgsEnum);
                initializersWalker.VisitInitializer(injectionsCtx, initializer);
                continue;
            }

            if (line.Contains(OverrideStatement))
            {
                continue;
            }

            // When a code
            var len = 0;
            for (; len < line.Length && line[len] == ' '; len++)
            {
            }

            code.AppendLine(line);
        }

        if (factoryRewriter.IsFinishMarkRequired)
        {
            code.AppendLine($"{finishLabel}:;");
        }

        ctx.Code.AppendLines(ctx.BuildTools.OnCreated(ctx, variable));
    }

    private void BuildOverrides(BuildContext ctx, DpFactory factory, ImmutableArray<DpOverride> overrides, LinesBuilder code)
    {
        foreach (var @override in overrides.OrderBy(i => i.Source.Position).Select(i => factory.ResolveOverride(i)))
        {
            code.AppendLine($"{variableNameProvider.GetOverrideVariableName(@override.Source)} = {@override.Source.ValueExpression};");
            overridesRegistry.Register(ctx.Root, @override);
        }
    }

    private static IEnumerable<IStatement> GetArgsStatements(IStatement statement)
    {
        var result = new Stack<IStatement>();
        var statements = new Stack<IStatement>();
        statements.Push(statement);
        while (statements.TryPop(out var nextStatement))
        {
            var variable = nextStatement.Current;
            if (variable.HasCycledReference && variable.Node.Lifetime == Lifetime.PerBlock)
            {
                continue;
            }

            result.Push(nextStatement);
            foreach (var arg in variable.Args)
            {
                statements.Push(arg);
            }
        }

        return result;
    }
}