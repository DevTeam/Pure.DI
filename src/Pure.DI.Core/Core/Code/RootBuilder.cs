// ReSharper disable SwitchStatementMissingSomeEnumCasesNoDefault
// ReSharper disable InvertIf
// ReSharper disable MergeIntoPattern
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ConvertIfStatementToReturnStatement
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
namespace Pure.DI.Core.Code;

using static Lifetime;
using static LinesExtensions;

class RootBuilder(
    INodeTools nodeTools,
    IBuildTools buildTools,
    IAccumulators accumulators,
    ILocks locks,
    Func<IReadOnlyCollection<VarInjection>, IVariablesWalker> varsWalkerFactory,
    IInjections inj,
    ITypeResolver typeResolver,
    ISymbolNames symbolNames,
    ICompilations compilations,
    Func<DpFactory, IFactoryValidator> factoryValidatorFactory,
    Func<InitializersWalkerContext, IInitializersWalker> initializersWalkerFactory,
    Func<FactoryRewriterContext, IFactoryRewriter> factoryRewriterFactory,
    ILocationProvider locationProvider,
    IUniqueNameProvider uniqueNameProvider,
    INameProvider nameProvider,
    IOverridesRegistry overridesRegistry,
    INameFormatter nameFormatter,
    ILocalFunctions localFunctions,
    CancellationToken cancellationToken)
    : IBuilder<RootContext, VarInjection>
{
    public static readonly ParenthesizedLambdaExpressionSyntax DefaultBindAttrParenthesizedLambda = SyntaxFactory.ParenthesizedLambdaExpression();
    public static readonly ParameterSyntax DefaultCtxParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("ctx_1182D127"));
    private static readonly string InjectionStatement = $"{Names.InjectionMarker};";
    private static readonly string InitializationStatement = $"{Names.InitializationMarker};";
    private static readonly string OverrideStatement = $"{Names.OverrideMarker};";

    public VarInjection Build(RootContext rootContext)
    {
        var rootVarsMap = rootContext.VarsMap;
        var rootVarInjection = rootVarsMap.GetInjection(rootContext.Root.Injection, rootContext.Root.Node);
        var lines = new Lines();
        var ctx = new CodeContext(
            rootContext,
            ImmutableArray<VarInjection>.Empty,
            rootVarInjection,
            rootContext.VarsMap,
            rootContext.IsThreadSafeEnabled,
            lines,
            accumulators.CreateAccumulators(accumulators.GetAccumulators(rootContext.Graph.Graph, rootContext.Root.Node), rootVarsMap).ToImmutableArray(),
            []);

        accumulators.BuildAccumulators(ctx);
        BuildCode(ctx);
        rootVarInjection.Var.CodeExpression = buildTools.OnInjected(ctx, rootVarInjection);

        var setup = rootContext.Graph.Source;
        foreach (var perResolve in rootVarsMap.Declarations.Where(i => i.Node.Lifetime is PerResolve).OrderBy(i => i.Node.BindingId))
        {
            rootContext.Lines.AppendLine($"var {perResolve.Name} = default({typeResolver.Resolve(setup, perResolve.InstanceType)});");
            if (perResolve.InstanceType.IsValueType)
            {
                rootContext.Lines.AppendLine($"var {perResolve.Name}{Names.CreatedValueNameSuffix} = false;");
            }
        }

        rootContext.Lines.AppendLines(lines);
        return rootVarInjection;
    }

    private void BuildCode(CodeContext parentCtx)
    {
        var varInjection = parentCtx.VarInjection;
        var var = varInjection.Var;
        if (var.IsCreated)
        {
            if (parentCtx.IsFactory && !string.IsNullOrWhiteSpace(var.LocalFunctionName))
            {
                parentCtx.Lines.AppendLine($"{var.LocalFunctionName}();");
                return;
            }

#if DEBUG
            parentCtx.Lines.AppendComments($"{var.Name}: skip");
#endif
            return;
        }

        if (!string.IsNullOrEmpty(var.LocalFunctionName))
        {
            parentCtx.Lines.AppendLine($"{var.LocalFunctionName}();");
            var.Declaration.IsDeclared = true;
            return;
        }

        var lines = new Lines();
        var varCtx = parentCtx with
        {
            Lines = lines,
            ContextTag = ReferenceEquals(varInjection.Injection.Tag, MdTag.ContextTag) ? parentCtx.ContextTag : varInjection.Injection.Tag
        };

        var varsMap = varCtx.VarsMap;
        var setup = varCtx.RootContext.Graph.Source;
        var isBlock = nodeTools.IsBlock(var.AbstractNode);
        var isLazy = nodeTools.IsLazy(var.AbstractNode.Node);
        var.HasCycle ??= IsCycle(varCtx.RootContext.Graph.Graph, var.Declaration.Node.Node, ImmutableHashSet<DependencyNode>.Empty);
        var acc = isLazy ? accumulators.GetAccumulators(varCtx.RootContext.Graph.Graph, var.AbstractNode).ToImmutableArray() : ImmutableArray<(MdAccumulator, Dependency)>.Empty;
        var isLocalFunction = localFunctions.UseFor(varCtx);
        var mapToken =
            isLocalFunction
                ? varsMap.LocalFunction(var, lines)
                : isLazy
                    ? varsMap.Lazy(var, lines)
                    : isBlock
                        ? varsMap.Block(var, lines)
                        : Disposables.Empty;

        if (isLocalFunction || isLazy)
        {
            varCtx = varCtx with { IsLockRequired = varCtx.RootContext.IsThreadSafeEnabled };
        }

#if DEBUG
        varCtx.Lines.AppendComments($"{var.Name}: {nameof(var.Declaration.IsDeclared)}={var.Declaration.IsDeclared}, {nameof(var.IsCreated)}={var.IsCreated}, {nameof(var.HasCycle)}={var.HasCycle}, {nameof(varCtx.IsLockRequired)}={varCtx.IsLockRequired}, {nameof(isLazy)}={isLazy}, {nameof(isBlock)}={isBlock}, {nameof(acc)}={acc.Length}");
#endif

        if (isBlock)
        {
            StartSingleInstanceCheck(varCtx with { Lines = lines });
            varCtx = varCtx with { IsLockRequired = false };
        }

        var ctx = varCtx;
        if (isLazy)
        {
            ctx = ctx with { Accumulators = ctx.Accumulators.AddRange(accumulators.CreateAccumulators(acc, varsMap)), IsFactory = false };
            ctx.Overrides.Clear();
            accumulators.BuildAccumulators(ctx);
        }

        var varInjections = new List<VarInjection>();
        var.IsCreated = true;

        // Implementation
        if (var.AbstractNode.Node.Implementation is {} implementation)
        {
            if (ctx.RootContext.Graph.Graph.TryGetInEdges(var.AbstractNode.Node, out var implementationDependencies))
            {
                var injections = implementationDependencies.Select(dependency => varsMap.GetInjection(dependency.Injection, dependency.Source)).ToList();
                foreach (var dependencyVar in SortInjections(injections))
                {
                    BuildCode(ctx.CreateChild(dependencyVar));
                }

                varInjections.AddRange(injections);
            }

            var varsWalker = varsWalkerFactory(varInjections);
            varsWalker.VisitConstructor(Unit.Shared, implementation.Constructor);
            var ctorArgs = varsWalker.GetResult();

            var requiredFields = ImmutableArray.CreateBuilder<(VarInjection RequiredVarInjection, DpField RequiredField)>();
            foreach (var requiredField in implementation.Fields.Where(i => i.Field.IsRequired).OrderBy(i => i.Ordinal ?? int.MaxValue - 1))
            {
                varsWalker.VisitField(Unit.Shared, requiredField, null);
                var dependencyVar = varsWalker.GetResult().Single();
                requiredFields.Add((dependencyVar, requiredField));
            }

            var requiredProperties = ImmutableArray.CreateBuilder<(VarInjection RequiredVarInjection, DpProperty RequiredProperty)>();
            foreach (var requiredProperty in implementation.Properties.Where(i => i.Property.IsRequired || i.Property.SetMethod?.IsInitOnly == true).OrderBy(i => i.Ordinal ?? int.MaxValue))
            {
                varsWalker.VisitProperty(Unit.Shared, requiredProperty, null);
                var dependencyVar = varsWalker.GetResult().Single();
                requiredProperties.Add((dependencyVar, requiredProperty));
            }

            var visits = new List<(Action<CodeContext, string> Run, int? Ordinal)>();
            foreach (var field in implementation.Fields.Where(i => !i.Field.IsRequired))
            {
                varsWalker.VisitField(Unit.Shared, field, null);
                var dependencyVar = varsWalker.GetResult().Single();
                visits.Add((VisitFieldAction, field.Ordinal));
                continue;

                void VisitFieldAction(CodeContext context, string name) => inj.FieldInjection(name, context, field, dependencyVar);
            }

            foreach (var property in implementation.Properties.Where(i => !i.Property.IsRequired && i.Property.SetMethod?.IsInitOnly != true))
            {
                varsWalker.VisitProperty(Unit.Shared, property, null);
                var dependencyVar = varsWalker.GetResult().Single();
                visits.Add((VisitFieldAction, property.Ordinal));
                continue;

                void VisitFieldAction(CodeContext context, string name) => inj.PropertyInjection(name, context, property, dependencyVar);
            }

            foreach (var method in implementation.Methods)
            {
                varsWalker.VisitMethod(Unit.Shared, method, null);
                var methodVars = varsWalker.GetResult();
                visits.Add((VisitMethodAction, method.Ordinal));
                continue;

                void VisitMethodAction(CodeContext context, string name) => inj.MethodInjection(name, context, method, methodVars);
            }

            var onCreatedStatements = buildTools.OnCreated(ctx, varInjection);
            var hasOnCreatedStatements = buildTools.OnCreated(ctx, varInjection).Count > 0;
            var hasAlternativeInjections = visits.Count > 0;
            var tempVariableInit =
                ctx.RootContext.IsThreadSafeEnabled
                && var.AbstractNode.Lifetime is not Transient and not PerBlock
                && (hasAlternativeInjections || hasOnCreatedStatements);

            var tempVar = var;
            if (tempVariableInit)
            {
                tempVar = var with { NameOverride = $"{var.Declaration.Name}{Names.TempInstanceValueNameSuffix}" };
                lines.AppendLine($"{typeResolver.Resolve(ctx.RootContext.Graph.Source, tempVar.InstanceType)} {tempVar.Name};");
                if (onCreatedStatements.Count > 0)
                {
                    onCreatedStatements = buildTools.OnCreated(ctx, varInjection with { Var = tempVar });
                }
            }

            var instantiation = CreateInstantiation(ctx, ctorArgs, requiredFields, requiredProperties);
            if (var.AbstractNode.Lifetime is not Transient
                || hasAlternativeInjections
                || tempVariableInit
                || hasOnCreatedStatements)
            {
                lines.Append($"{buildTools.GetDeclaration(ctx, var.Declaration, useVar: true)}{tempVar.Name} = ");
                lines.Append(instantiation);
                lines.AppendLine(";");
            }
            else
            {
                var.CodeExpression = instantiation;
            }

            foreach (var visit in visits.OrderBy(i => i.Ordinal ?? int.MaxValue))
            {
                cancellationToken.ThrowIfCancellationRequested();
                visit.Run(ctx, tempVar.Name);
            }

            lines.AppendLines(onCreatedStatements);
            if (tempVariableInit)
            {
                lines.AppendLine($"{Names.SystemNamespace}Threading.Thread.MemoryBarrier();");
                lines.AppendLine($"{var.Name} = {tempVar.Name};");
            }
        }
        else
        {
            if (var.AbstractNode.Node.Factory is {} factory)
            {
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
                else
                {
                    ctx = ctx with { IsFactory = true };
                }

                // Rewrites syntax tree
                var finishLabel = $"{var.Declaration.Name}Finish";
                var factoryExpression = (LambdaExpressionSyntax)factory.Source.LocalVariableRenamingRewriter.Clone().Rewrite(setup.SemanticModel, false, originalLambda);
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

                var textLines = new List<TextLine>();
                var hasOverridesLock = false;
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

                    foreach (var statement in curBlock.Statements)
                    {
                        var text = statement.GetText();
                        textLines.AddRange(text.Lines);
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
                    textLines.AddRange(text.Lines);
                }

                if (ctx.RootContext.Graph.Graph.TryGetInEdges(var.AbstractNode.Node, out var dependencies))
                {
                    // ReSharper disable once LoopCanBeConvertedToQuery
                    foreach (var dependency in dependencies)
                    {
                        var dependencyVar = varsMap.GetInjection(dependency.Injection, dependency.Source);
                        varInjections.Add(dependencyVar);
                    }
                }

                var injectionArgs = varInjections.Where(i => i.Injection.Kind is InjectionKind.FactoryInjection).ToList();
                var initializationArgs = varInjections.Where(i => i.Injection.Kind != InjectionKind.FactoryInjection).ToList();

                // Replaces injection markers by injection code
                if (injectionArgs.Count != injections.Count)
                {
                    throw new CompileErrorException(
                        string.Format(Strings.Error_Template_LifetimeDoesNotSupportCyclicDependencies, var.AbstractNode.Lifetime),
                        ImmutableArray.Create(locationProvider.GetLocation(factory.Source.Source)),
                        LogId.ErrorInvalidMetadata);
                }

                if (factory.Initializers.Length != inits.Count)
                {
                    throw new CompileErrorException(
                        Strings.Error_InvalidNumberOfInitializers,
                        ImmutableArray.Create(locationProvider.GetLocation(factory.Source.Source)),
                        LogId.ErrorInvalidMetadata);
                }

                using var resolvers = injections
                    .Zip(injectionArgs, (injection, argument) => (injection, argument))
                    .Zip(factory.Resolvers, (i, resolver) => (i.injection, i.argument, resolver))
                    .GetEnumerator();

                using var initializers = inits
                    .Zip(factory.Initializers, (initialization, initializer) => (initialization, initializer))
                    .GetEnumerator();

                var initializationArgsEnum = initializationArgs.GetEnumerator();
                var linePrefixes = new List<(string line, int prefixLength)>();
                foreach (var textLine in textLines)
                {
                    var line = textLine.ToString();
                    var length = line.TakeWhile(char.IsWhiteSpace).Count();
                    var prefixLength = 0;
                    for (var i = 0; i < length; i++)
                    {
                        switch (line[i])
                        {
                            case '\t':
                                prefixLength += 4;
                                break;

                            default:
                                prefixLength++;
                                break;
                        }
                    }

                    line = line[length..];
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    linePrefixes.Add((line, prefixLength >> 1));
                }

                if (fixFirstLinePrefix && linePrefixes.Count > 1)
                {
                    linePrefixes[0] = linePrefixes[0] with { prefixLength = linePrefixes[1].prefixLength };
                }

                var indents = linePrefixes
                    .GroupBy(i => i.prefixLength)
                    .OrderBy(i => i.Key)
                    .Select((i, index) => (prefix: i.Key, indent: index))
                    .ToDictionary(i => i.prefix, i => i.indent);

                foreach (var (line, prefixLength) in linePrefixes)
                {
                    if (!indents.TryGetValue(prefixLength, out var indent))
                    {
                        indent = 0;
                    }

                    using (lines.Indent(indent))
                    {
                        if (line.Contains(InjectionStatement) && resolvers.MoveNext())
                        {
                            // When an injection marker
                            var (injection, argument, resolver) = resolvers.Current;
                            if (hasOverrides)
                            {
                                BuildOverrides(ctx, factory, resolver.Overrides, lines);
                            }

                            BuildCode(ctx.CreateChild(argument));
                            lines.AppendLine($"{(injection.DeclarationRequired ? $"{typeResolver.Resolve(setup, argument.Injection.Type)} " : "")}{injection.VariableName} = {buildTools.OnInjected(ctx, argument)};");

                            continue;
                        }

                        if (line.Contains(InitializationStatement) && initializers.MoveNext())
                        {
                            var (initialization, initializer) = initializers.Current;
                            if (hasOverrides)
                            {
                                BuildOverrides(ctx, factory, initializer.Overrides, lines);
                            }

                            var initCtx = ctx;
                            var initializersWalker = initializersWalkerFactory(
                                new InitializersWalkerContext(
                                    i => BuildCode(initCtx.CreateChild(i)),
                                    initialization.VariableName,
                                    initializationArgsEnum));
                            initializersWalker.VisitInitializer(ctx, initializer);
                            continue;
                        }

                        if (line.Contains(OverrideStatement))
                        {
                            continue;
                        }

                        lines.AppendLine(line);
                    }
                }

                if (factoryRewriter.IsFinishMarkRequired)
                {
                    lines.AppendLine($"{finishLabel}:;");
                }

                lines.AppendLines(buildTools.OnCreated(ctx, varInjection));

                if (hasOverridesLock)
                {
                    lines.DecIndent();
                    lines.AppendLine(BlockFinish);
                }
            }
            else
            {
                if (var.AbstractNode.Construct is {} construct)
                {
                    switch (construct.Source.Kind)
                    {
                        case MdConstructKind.Enumerable:
                        case MdConstructKind.AsyncEnumerable:
                            var localMethodName = $"{Names.EnumerateMethodNamePrefix}_{var.Declaration.Name}".Replace("__", "_");
                            if (compilations.GetLanguageVersion(construct.Source.SemanticModel.Compilation) >= LanguageVersion.CSharp9)
                            {
                                buildTools.AddAggressiveInlining(lines);
                            }

                            var methodPrefix = construct.Source.Kind == MdConstructKind.AsyncEnumerable ? "async " : "";
                            lines.AppendLine($"{methodPrefix}{typeResolver.Resolve(ctx.RootContext.Graph.Source, var.InstanceType)} {localMethodName}()");
                            using (lines.CreateBlock())
                            {
                                var hasYieldReturn = false;
                                if (ctx.RootContext.Graph.Graph.TryGetInEdges(var.AbstractNode.Node, out var enumerableDependencies))
                                {
                                    foreach (var dependency in enumerableDependencies)
                                    {
                                        var dependencyVar = varsMap.GetInjection(dependency.Injection, dependency.Source);
                                        varInjections.Add(dependencyVar);
                                        BuildCode(ctx.CreateChild(dependencyVar));
                                        lines.AppendLine($"yield return {buildTools.OnInjected(ctx, dependencyVar)};");
                                        hasYieldReturn = true;
                                    }
                                }

                                if (methodPrefix == "async ")
                                {
                                    lines.AppendLine("await Task.CompletedTask;");
                                }
                                else
                                {
                                    if (!hasYieldReturn)
                                    {
                                        lines.AppendLine("yield break;");
                                    }
                                }
                            }

                            lines.AppendLine();
                            lines.AppendLine($"{buildTools.GetDeclaration(ctx, var.Declaration, useVar: true)}{var.Name} = {localMethodName}();");
                            lines.AppendLines(buildTools.OnCreated(ctx, varInjection));
                            break;

                        case MdConstructKind.Array:
                            if (ctx.RootContext.Graph.Graph.TryGetInEdges(var.AbstractNode.Node, out var arrayDependencies))
                            {
                                var injections = arrayDependencies.Select(dependency => varsMap.GetInjection(dependency.Injection, dependency.Source)).ToList();
                                foreach (var dependencyVar in SortInjections(injections))
                                {
                                    BuildCode(ctx.CreateChild(dependencyVar));
                                }

                                varInjections.AddRange(injections);
                            }

                            var instantiation = $"new {typeResolver.Resolve(ctx.RootContext.Graph.Source, construct.Source.ElementType)}[{varInjections.Count.ToString()}] {{ {string.Join(", ", varInjections.Select(item => buildTools.OnInjected(ctx, item)))} }}";
                            var onCreated = buildTools.OnCreated(ctx, varInjection);
                            if (onCreated.Count > 0)
                            {
                                lines.AppendLine($"{buildTools.GetDeclaration(ctx, var.Declaration, useVar: true)}{var.Name} = {instantiation};");
                                lines.AppendLines(onCreated);
                            }
                            else
                            {
                                var.CodeExpression =  instantiation;
                            }
                            break;

                        case MdConstructKind.Span:
                            if (ctx.RootContext.Graph.Graph.TryGetInEdges(var.AbstractNode.Node, out var spanDependencies))
                            {
                                var injections = spanDependencies.Select(dependency => varsMap.GetInjection(dependency.Injection, dependency.Source)).ToList();
                                foreach (var dependencyVar in SortInjections(injections))
                                {
                                    BuildCode(ctx.CreateChild(dependencyVar));
                                }

                                varInjections.AddRange(injections);
                            }

                            var createArray = $"{typeResolver.Resolve(ctx.RootContext.Graph.Source, construct.Source.ElementType)}[{varInjections.Count.ToString()}] {{ {string.Join(", ", varInjections.Select(item => buildTools.OnInjected(ctx, item)))} }}";

                            var isStackalloc =
                                construct.Source.ElementType.IsValueType
                                && spanDependencies.Count <= Const.MaxStackalloc
                                && compilations.GetLanguageVersion(construct.Binding.SemanticModel.Compilation) >= LanguageVersion.CSharp7_3;

                            var createInstance = isStackalloc ? $"stackalloc {createArray}" : $"new {Names.SystemNamespace}Span<{typeResolver.Resolve(ctx.RootContext.Graph.Source, construct.Source.ElementType)}>(new {createArray})";
                            lines.AppendLine($"{buildTools.GetDeclaration(ctx, var.Declaration)}{var.Name} = {createInstance};");
                            lines.AppendLines(buildTools.OnCreated(ctx, varInjection));
                            break;

                        case MdConstructKind.Composition:
                            lines.AppendLine($"{buildTools.GetDeclaration(ctx, var.Declaration, useVar: true)}{var.Name} = this;");
                            break;

                        case MdConstructKind.OnCannotResolve:
                            lines.AppendLine($"{buildTools.GetDeclaration(ctx, var.Declaration, useVar: true)}{var.Name} = {Names.OnCannotResolve}<{varInjection.ContractType}>({varInjection.Injection.Tag.ValueToString()}, {var.AbstractNode.Lifetime.ValueToString()});");
                            break;

                        case MdConstructKind.ExplicitDefaultValue:
                            lines.AppendLine($"{buildTools.GetDeclaration(ctx, var.Declaration)}{var.Name} = {construct.Source.ExplicitDefaultValue.ValueToString()};");
                            break;

                        case MdConstructKind.Accumulator:
                        case MdConstructKind.Override:
                            break;

                        case MdConstructKind.None:
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        if (isBlock)
        {
            FinishSingleInstanceCheck(varCtx with { IsLockRequired = parentCtx.IsLockRequired });
        }

        mapToken.Dispose();

        if (isLocalFunction)
        {
            var baseName = nameFormatter.Format("{type}{tag}", varCtx.VarInjection.Var.InstanceType, varCtx.VarInjection.Injection.Tag);
            var localFunction = var.LocalFunction;
            if (compilations.GetLanguageVersion(varCtx.RootContext.Graph.Source.SemanticModel.Compilation) >= LanguageVersion.CSharp9)
            {
                buildTools.AddAggressiveInlining(localFunction);
            }

            var.LocalFunctionName = uniqueNameProvider.GetUniqueName($"Ensure{baseName}Exists{Names.Salt}");
            localFunction.AppendLine($"void {var.LocalFunctionName}()");
            using (localFunction.CreateBlock())
            {
                localFunction.AppendLines(lines);
            }

            lines = new Lines();
            lines.AppendLine($"{var.LocalFunctionName}();");
        }

        parentCtx.Lines.AppendLines(lines);
        var.Declaration.IsDeclared = true;
    }

    private static IEnumerable<VarInjection> SortInjections(List<VarInjection> injections) => injections.OrderBy(GetInjectionPriority);

    private static int GetInjectionPriority(VarInjection varInjection)
    {
        var node = varInjection.Var.AbstractNode;
        if (node.Arg is not null)
        {
            return 1;
        }

        if (varInjection.Var.HasCycle == true)
        {
            return 2;
        }

        if (node.Node.Implementation is not null)
        {
            return node.Lifetime switch
            {
                PerBlock => 10,
                Singleton => 11,
                Scoped => 12,
                PerResolve => 13,
                _ => 14
            };
        }

        if (node.Construct is { } construct)
        {
            return construct.Source.Kind switch
            {
                MdConstructKind.Accumulator => 0,
                MdConstructKind.ExplicitDefaultValue => 1,
                MdConstructKind.Composition => 1,
                MdConstructKind.OnCannotResolve => 20,
                MdConstructKind.Enumerable => 22,
                MdConstructKind.Array => 21,
                MdConstructKind.Span => 21,
                MdConstructKind.AsyncEnumerable => 22,
                MdConstructKind.Override => 28,
                _ => 29
            };
        }

        if (node.Node.Factory is {} factory)
        {
            return 30 + factory.Resolvers.Length + factory.Initializers.Length * 100 + (factory.HasOverrides ? 1000 : 0);
        }

        return int.MaxValue;
    }

    private void StartSingleInstanceCheck(CodeContext ctx)
    {
        var isLockRequired = ctx.IsLockRequired;
        var var = ctx.VarInjection.Var;
        var lines = ctx.Lines;
        var compilation = var.AbstractNode.Binding.SemanticModel.Compilation;
        var checkExpression = var.InstanceType.IsValueType
            ? $"!{var.Name}{Names.CreatedValueNameSuffix}"
            : buildTools.NullCheck(compilation, var.Name);

        lines.AppendLine($"if ({checkExpression})");
        if (isLockRequired)
        {
            lines.IncIndent();
            locks.AddLockStatements(ctx.RootContext.Root.IsStatic, lines, false);
            lines.IncIndent();
            lines.AppendLine($"if ({checkExpression})");
        }

        lines.AppendLine(BlockStart);
        lines.IncIndent();
    }

    private void FinishSingleInstanceCheck(CodeContext ctx)
    {
        var var = ctx.VarInjection.Var;
        var lines = ctx.Lines;
        if (var.AbstractNode.Lifetime is Singleton or Scoped && nodeTools.IsDisposableAny(var.AbstractNode.Node))
        {
            var parent = "";
            if (var.AbstractNode.Lifetime == Singleton)
            {
                parent = $"{Names.RootFieldName}.";
            }

            lines.AppendLine($"{parent}{Names.DisposablesFieldName}[{parent}{Names.DisposeIndexFieldName}++] = {var.Name};");
        }

        if (var.InstanceType.IsValueType)
        {
            if (var.AbstractNode.Lifetime is not Transient and not PerBlock && ctx.RootContext.IsThreadSafeEnabled)
            {
                lines.AppendLine($"{Names.SystemNamespace}Threading.Thread.MemoryBarrier();");
            }

            lines.AppendLine($"{var.Name}{Names.CreatedValueNameSuffix} = true;");
        }

        lines.DecIndent();
        lines.AppendLine(BlockFinish);
        if (ctx.IsLockRequired)
        {
            lines.DecIndent();
            lines.DecIndent();
        }

        lines.AppendLine();
    }

    private string CreateInstantiation(
        CodeContext ctx,
        IReadOnlyCollection<VarInjection> ctorArgs,
        IReadOnlyCollection<(VarInjection RequiredVariable, DpField RequiredField)> requiredFields,
        IReadOnlyCollection<(VarInjection RequiredVariable, DpProperty RequiredProperty)> requiredProperties)
    {
        var var = ctx.VarInjection.Var;
        var code = new StringBuilder();
        var required = requiredFields.Select(i => (Variable: i.RequiredVariable, i.RequiredField.Field.Name))
            .Concat(requiredProperties.Select(i => (Variable: i.RequiredVariable, i.RequiredProperty.Property.Name)))
            .ToList();

        var args = string.Join(", ", ctorArgs.Select(i => buildTools.OnInjected(ctx, i)));
        code.Append(var.InstanceType.IsTupleType ? $"({args})" : $"new {typeResolver.Resolve(ctx.RootContext.Graph.Source, var.InstanceType)}({args})");
        if (required.Count > 0)
        {
            code.Append($" {BlockStart} ");
            for (var index = 0; index < required.Count; index++)
            {
                var (v, name) = required[index];
                code.Append($"{name} = {buildTools.OnInjected(ctx, v)}{(index < required.Count - 1 ? ", " : "")}");
            }

            code.Append($" {BlockFinish}");
        }

        return code.ToString();
    }

    private static bool IsCycle(
        IGraph<DependencyNode, Dependency> graph,
        DependencyNode node,
        ImmutableHashSet<DependencyNode> processed)
    {
        if (processed.Contains(node))
        {
            return true;
        }

        processed = processed.Add(node);
        if (graph.TryGetInEdges(node, out var dependencies))
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var dependency in dependencies)
            {
                if (IsCycle(graph, dependency.Source, processed))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void BuildOverrides(CodeContext ctx, DpFactory factory, ImmutableArray<DpOverride> overrides, Lines lines)
    {
        foreach (var @override in overrides.OrderBy(i => i.Source.Position).Select(i => factory.ResolveOverride(i)))
        {
            var name = nameProvider.GetOverrideVariableName(@override.Source);
            var isDeclared = !ctx.Overrides.Add(name);
            lines.AppendLine($"{(isDeclared ? "" : $"{typeResolver.Resolve(ctx.RootContext.Graph.Source, @override.Source.ContractType)} ")}{name} = {@override.Source.ValueExpression};");
            overridesRegistry.Register(ctx.RootContext.Root, @override);
        }
    }
}