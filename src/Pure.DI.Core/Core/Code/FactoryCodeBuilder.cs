namespace Pure.DI.Core.Code;

using System.Collections;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static LinesExtensions;

sealed class FactoryCodeBuilder(
    Func<IBuilder<CodeContext, IEnumerator>> variablesCodeBuilderFactory,
    IBuildTools buildTools,
    ITypeResolver typeResolver,
    ISymbolNames symbolNames,
    Func<DpFactory, IFactoryValidator> factoryValidatorFactory,
    Func<InitializersWalkerContext, IInitializersWalker> initializersWalkerFactory,
    Func<FactoryRewriterContext, IFactoryRewriter> factoryRewriterFactory,
    ILocationProvider locationProvider,
    ILocks locks,
    INameProvider nameProvider,
    IOverridesRegistry overridesRegistry,
    INodeTools nodeTools)
    : IBuilder<CodeBuilderContext, IEnumerator>
{
    private static readonly string InjectionStatement = $"{Names.InjectionMarker};";
    private static readonly string InitializationStatement = $"{Names.InitializationMarker};";
    private static readonly string OverrideStatement = $"{Names.OverrideMarker};";

    public IEnumerator Build(CodeBuilderContext data)
    {
        var (ctx, varInjections) = data;
        var varInjection = ctx.VarInjection;
        var var = varInjection.Var;
        var lines = ctx.Lines;
        var factory = var.AbstractNode.Node.Factory!;
        var setup = ctx.RootContext.Graph.Source;
        var varsMap = ctx.VarsMap;
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
                            SyntaxFactory.IdentifierName(RootBuilder.DefaultCtxParameter.Identifier),
                            SyntaxFactory.IdentifierName(nameof(IContext.Inject))))
                    .AddArgumentListArguments(valueArg);

                block.Add(SyntaxFactory.ExpressionStatement(injection));
            }

            if (factory.Source.MemberResolver is { Member: {} member, TypeConstructor: {} typeConstructor } memberResolver)
            {
                ExpressionSyntax? value = null;
                var type = memberResolver.ContractType;
                var instance = member.IsStatic
                    ? SyntaxFactory.ParseTypeName(symbolNames.GetGlobalName(type))
                    : SyntaxFactory.IdentifierName(Names.DefaultInstanceValueName);

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
                            var binding = var.AbstractNode.Binding;
                            var typeArgs = new List<TypeSyntax>();
                            // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
                            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
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

            originalLambda = SyntaxFactory.SimpleLambdaExpression(RootBuilder.DefaultCtxParameter)
                .WithBlock(SyntaxFactory.Block(block));
        }
        else
        {
            ctx = ctx with { IsFactory = true };
        }

        // Rewrites syntax tree
        var finishLabel = $"{var.Declaration.Name}Finish";
        var localVariableRenamingRewriter = factory.Source.LocalVariableRenamingRewriter.Clone();
        var factoryExpression = (LambdaExpressionSyntax)localVariableRenamingRewriter.Rewrite(setup.SemanticModel, false, originalLambda);
        var injections = new List<FactoryRewriter.Injection>();
        var inits = new List<FactoryRewriter.Initializer>();
        var rewriterContext = new FactoryRewriterContext(factory, varInjection, finishLabel, injections, inits);
        var factoryRewriter = factoryRewriterFactory(rewriterContext);
        var lambda = factoryRewriter.Rewrite(ctx, factoryExpression);
        factoryValidatorFactory(factory).Visit(lambda);
        SyntaxNode syntaxNode = lambda.Block is not null ? lambda.Block : SyntaxFactory.ExpressionStatement((ExpressionSyntax)lambda.Body);
        var hasOverrides = factory.HasOverrides;
        if (hasOverrides)
        {
            ctx = ctx with { HasOverrides = true };
        }

        if (!var.Declaration.IsDeclared && (var.HasCycle ?? false))
        {
            lines.AppendLine($"var {var.Name} = default({typeResolver.Resolve(ctx.RootContext.Graph.Source, var.InstanceType)});");
            var.Declaration.IsDeclared = true;
        }

        var linePrefixes = new List<LinePrefix>();
        var hasOverridesLock = false;
        var isLazy = nodeTools.IsLazy(var.AbstractNode.Node, ctx.RootContext.Graph);
        if (hasOverrides && ctx.IsLockRequired && !isLazy)
        {
            if (!var.Declaration.IsDeclared)
            {
                lines.AppendLine($"{buildTools.GetDeclaration(ctx, var.Declaration)}{var.Name};");
                var.Declaration.IsDeclared = true;
            }

            locks.AddLockStatements(ctx.RootContext.Root.IsStatic, lines, false);
            lines.AppendLine(BlockStart);
            lines.IncIndent();
            ctx = ctx with { IsLockRequired = false };
            hasOverridesLock = true;
        }

        var fixFirstLinePrefix = false;
        if (syntaxNode is BlockSyntax curBlock)
        {
            if (!var.Declaration.IsDeclared)
            {
                lines.AppendLine($"{buildTools.GetDeclaration(ctx, var.Declaration)}{var.Name};");
                var.Declaration.IsDeclared = true;
            }

            foreach (var text in curBlock.Statements.Select(statement => statement.GetText()))
            {
                AddLinePrefixes(text, linePrefixes);
            }
        }
        else
        {
            var leadingTrivia = syntaxNode.GetLeadingTrivia().ToFullString().TrimStart();
            if (!string.IsNullOrEmpty(leadingTrivia))
            {
                lines.Append(leadingTrivia);
            }
            else
            {
                fixFirstLinePrefix = true;
            }

            if (!var.Declaration.IsDeclared)
            {
                lines.Append($"{buildTools.GetDeclaration(ctx, var.Declaration)}{var.Name} = ");
                var.Declaration.IsDeclared = true;
            }
            else
            {
                lines.Append($"{var.Name} = ");
            }

            var text = syntaxNode.WithoutTrivia().GetText();
            AddLinePrefixes(text, linePrefixes);
        }

        List<VarInjection>? injectionArgs = null;
        List<VarInjection>? initializationArgs = null;
        if (ctx.RootContext.Graph.Graph.TryGetInEdges(var.AbstractNode.Node, out var dependencies))
        {
            if (varInjections is List<VarInjection> varList)
            {
                var desiredCapacity = varList.Count + dependencies.Count;
                if (varList.Capacity < desiredCapacity)
                {
                    varList.Capacity = desiredCapacity;
                }
            }

            foreach (var dependency in dependencies)
            {
                var dependencyVar = varsMap.GetInjection(ctx.RootContext.Graph, dependency.Injection, dependency.Source);
                varInjections.Add(dependencyVar);
                if (dependencyVar.Injection.Kind is InjectionKind.FactoryInjection)
                {
                    (injectionArgs ??= new List<VarInjection>(dependencies.Count)).Add(dependencyVar);
                }
                else
                {
                    (initializationArgs ??= new List<VarInjection>(dependencies.Count)).Add(dependencyVar);
                }
            }
        }
        injectionArgs ??= [];
        initializationArgs ??= [];
            
        if (injections.Count != factory.Resolvers.Length
            || injections.Count != injectionArgs.Count)
        {
            throw new CompileErrorException(
                string.Format(Strings.Error_Template_LifetimeDoesNotSupportCyclicDependencies, var.AbstractNode.ActualLifetime),
                ImmutableArray.Create(locationProvider.GetLocation(factory.Source.Source)),
                LogId.ErrorLifetimeDoesNotSupportCyclicDependencies,
                nameof(Strings.Error_Template_LifetimeDoesNotSupportCyclicDependencies));
        }

        if (factory.Initializers.Length != inits.Count)
        {
            throw new CompileErrorException(
                Strings.Error_InvalidNumberOfInitializers,
                ImmutableArray.Create(locationProvider.GetLocation(factory.Source.Source)),
                LogId.ErrorInvalidNumberOfInitializers,
                nameof(Strings.Error_InvalidNumberOfInitializers));
        }

        var resolversCount = injections.Count;
        var initsCount = inits.Count;
        var resolversIdx = 0;
        var initsIdx = 0;
        var initializationArgsIdx = new StrongBox<int>(0);
        if (fixFirstLinePrefix && linePrefixes.Count > 1)
        {
            linePrefixes[0] = linePrefixes[0] with { PrefixLength = linePrefixes[1].PrefixLength };
        }

        var indents = new Dictionary<int, int>();
        var indentIndex = 0;
        foreach (var linePrefix in linePrefixes.OrderBy(i => i.PrefixLength))
        {
            if (indents.ContainsKey(linePrefix.PrefixLength))
            {
                continue;
            }

            indents.Add(linePrefix.PrefixLength, indentIndex++);
        }

        for (var i = 0; i < linePrefixes.Count; i++)
        {
            var linePrefix = linePrefixes[i];
            if (!indents.TryGetValue(linePrefix.PrefixLength, out var indent))
            {
                indent = 0;
            }

            using (lines.Indent(indent))
            {
                var lineSpan = linePrefix.Line.Span;
                var marker = lineSpan.Trim();
                // Replaces injection markers by injection code
                if (marker.SequenceEqual(InjectionStatement.AsSpan()) && resolversIdx < resolversCount)
                {
                    // When an injection marker
                    var (injection, argument) = (injections[resolversIdx], injectionArgs[resolversIdx]);
                    var resolver = factory.Resolvers[resolversIdx];
                    resolversIdx++;

                    if (hasOverrides)
                    {
                        BuildOverrides(ctx, factory, localVariableRenamingRewriter, resolver.Overrides, lines);
                    }

                    yield return variablesCodeBuilderFactory().Build(ctx.CreateChild(argument));

                    var canInlineReturn =
                        injection.DeclarationRequired
                        && i + 1 < linePrefixes.Count
                        && IsReturnOfVariable(linePrefixes[i + 1].Line.Span, injection.VariableName);

                    if (canInlineReturn)
                    {
                        lines.AppendLine($"return {buildTools.OnInjected(ctx, argument)};");
                        i++; // Skip the return line
                    }
                    else
                    {
                        lines.AppendLine($"{(injection.DeclarationRequired ? $"{typeResolver.Resolve(setup, argument.Injection.Type)} " : "")}{injection.VariableName} = {buildTools.OnInjected(ctx, argument)};");
                    }

                    continue;
                }

                // Replaces initialization markers by initialization code
                if (marker.SequenceEqual(InitializationStatement.AsSpan()) && initsIdx < initsCount)
                {
                    var (initialization, initializer) = (inits[initsIdx], factory.Initializers[initsIdx]);
                    initsIdx++;
                        
                    if (hasOverrides)
                    {
                        BuildOverrides(ctx, factory, localVariableRenamingRewriter, initializer.Overrides, lines);
                    }

                    var initCtx = ctx;
                    var initializersWalker = initializersWalkerFactory(
                        new InitializersWalkerContext(
                            injection => variablesCodeBuilderFactory().Build(initCtx.CreateChild(injection)),
                            initialization.VariableName,
                            new FactoryInitializationArgsEnumerator(initializationArgs, initializationArgsIdx)));
                    yield return initializersWalker.VisitInitializer(ctx, initializer);
                    continue;
                }

                if (marker.SequenceEqual(OverrideStatement.AsSpan()))
                {
                    continue;
                }

                lines.AppendLine(lineSpan.ToString());
            }
        }

        if (factoryRewriter.IsFinishMarkRequired)
        {
            lines.AppendLine($"{finishLabel}:;");
        }

        lines.AppendLines(buildTools.OnCreated(ctx, varInjection));

        // ReSharper disable once InvertIf
        if (hasOverridesLock)
        {
            lines.DecIndent();
            lines.AppendLine(BlockFinish);
        }
    }

    private void BuildOverrides(CodeContext ctx, DpFactory factory, ILocalVariableRenamingRewriter localVariableRenamingRewriter, ImmutableArray<DpOverride> overrides, Lines lines)
    {
        foreach (var @override in overrides.OrderBy(i => i.Source.Position).Select(i => factory.ResolveOverride(i)))
        {
            var name = nameProvider.GetOverrideVariableName(@override.Source);
            var isDeclared = !ctx.Overrides.Add(name);
            var valueExpression = localVariableRenamingRewriter.Clone().Rewrite(ctx.RootContext.Graph.Source.SemanticModel, false, @override.Source.ValueExpression);
            lines.AppendLine($"{(isDeclared ? "" : $"{typeResolver.Resolve(ctx.RootContext.Graph.Source, @override.Source.ContractType)} ")}{name} = {valueExpression};");
            overridesRegistry.Register(ctx.RootContext.Root, @override);
        }
    }

    private static bool IsReturnOfVariable(ReadOnlySpan<char> line, string variableName)
    {
        var trimmed = line.Trim();
        if (!trimmed.StartsWith("return ", StringComparison.Ordinal) || !trimmed.EndsWith(";", StringComparison.Ordinal))
        {
            return false;
        }

        var nameSpan = trimmed["return ".Length..^1].Trim();
        return nameSpan.SequenceEqual(variableName.AsSpan());
    }

    private class FactoryInitializationArgsEnumerator(
        List<VarInjection> args,
        StrongBox<int> index)
        : IEnumerator<VarInjection>
    {
        public bool MoveNext()
        {
            if (index.Value >= args.Count)
            {
                return false;
            }

            Current = args[index.Value++];
            return true;

        }

        public void Reset() => throw new NotSupportedException();

        public VarInjection Current { get; private set; } = null!;

        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }
    }

    private record struct LinePrefix(ReadOnlyMemory<char> Line, int PrefixLength);

    private static void AddLinePrefixes(SourceText text, List<LinePrefix> linePrefixes)
    {
        foreach (var textLine in text.Lines)
        {
            var line = text.ToString(textLine.Span);
            var lineSpan = line.AsSpan();
            var length = 0;
            while (length < lineSpan.Length && char.IsWhiteSpace(lineSpan[length]))
            {
                length++;
            }

            var prefixLength = 0;
            for (var i = 0; i < length; i++)
            {
                switch (lineSpan[i])
                {
                    case '\t':
                        prefixLength += 4;
                        break;

                    default:
                        prefixLength++;
                        break;
                }
            }

            var contentSpan = lineSpan[length..];
            if (contentSpan.IsWhiteSpace())
            {
                continue;
            }

            linePrefixes.Add(new LinePrefix(line.AsMemory(length), prefixLength >> 1));
        }
    }
}
