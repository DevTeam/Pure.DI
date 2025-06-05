// ReSharper disable SwitchStatementMissingSomeEnumCasesNoDefault
// ReSharper disable InvertIf
// ReSharper disable MergeIntoPattern
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ConvertIfStatementToReturnStatement
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
namespace Pure.DI.Core.Code.v2;

using static Lifetime;
using static LinesBuilderExtensions;

class RootBuilder(
    INodeInfo nodeInfo,
    IBuildTools buildTools,
    ILocks locks,
    Func<IV2VariablesWalker> varsWalkerFactory,
    IInjections inj,
    ITypeResolver typeResolver,
    ISymbolNames symbolNames,
    IArguments arguments,
    ICompilations compilations,
    ITriviaTools triviaTools,
    Func<IFactoryValidator> factoryValidatorFactory,
    Func<IV2InitializersWalker> initializersWalkerFactory,
    ILocationProvider locationProvider,
    IVariableNameProvider variableNameProvider,
    IOverridesRegistry overridesRegistry,
    CancellationToken cancellationToken)
    : IBuilder<RootContext, Var>
{
    public const string DefaultInstanceValueName = "instance_1182D127";
    public static readonly ParenthesizedLambdaExpressionSyntax DefaultBindAttrParenthesizedLambda = SyntaxFactory.ParenthesizedLambdaExpression();
    public static readonly ParameterSyntax DefaultCtxParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier("ctx_1182D127"));
    private static readonly string InjectionStatement = $"{Names.InjectionMarker};";
    private static readonly string InitializationStatement = $"{Names.InitializationMarker};";
    private static readonly string OverrideStatement = $"{Names.OverrideMarker};";

    public Var Build(RootContext rootContext)
    {
        var varsMap = rootContext.VarsMap;
        var rootVar = varsMap.GetVar(rootContext.Root.Injection, rootContext.Root.Node, DeclarationPath.Root);
        var ctx = new CodeContext(
            rootContext,
            rootVar,
            DeclarationPath.Root,
            rootContext.VarsMap,
            rootContext.IsThreadSafeEnabled,
            rootContext.Lines,
            GetAccumulators(varsMap, rootContext.Graph.Graph, rootContext.Root.Node, DeclarationPath.Root).ToImmutableArray(),
            []);
        var changes = BuildCode(ctx);
        changes = new CodeChanges(Declarations: changes.Declarations.Add(rootVar.Declaration));
        rootVar.CodeExpression = buildTools.OnInjected(ctx, rootVar);
        varsMap.Reset(changes, true, DeclarationPath.Root);
        return rootVar;
    }

    private CodeChanges BuildCode(CodeContext parentCtx)
    {
        var changes = new CodeChanges(ImmutableArray<VarDeclaration>.Empty);
        var var = parentCtx.Var;
        if (var.Declaration.IsCreated)
        {
            return changes;
        }

        var lines = new LinesBuilder();
        var ctx = parentCtx with
        {
            Lines = lines,
            ContextTag = ReferenceEquals(var.Injection.Tag, MdTag.ContextTag) ? parentCtx.ContextTag : var.Injection.Tag
        };

        var varsMap = ctx.VarsMap;
        var setup = ctx.RootContext.Graph.Source;
        var isBlock = IsBlock(var.Node);
        var isLazy = IsLazy(var.Node);
        if (isBlock)
        {
            ctx = ctx with { IsLockRequired = false };
        }

        if (isBlock || isLazy)
        {
            ctx = ctx with { Path = ctx.Path.Append(var.Declaration) };
        }

        if (isLazy)
        {
            ctx = ctx with
            {
                IsLockRequired = ctx.RootContext.IsThreadSafeEnabled,
                Accumulators = GetAccumulators(varsMap, ctx.RootContext.Graph.Graph, var.Node, ctx.Path).ToImmutableArray()
            };

            if (!var.HasCycle && IsCycle(ctx))
            {
                var.HasCycle = true;
            }

            ctx.Overrides.Clear();
        }

        // Accumulators
        var accumulators = ctx.Accumulators
            .Where(acc => !acc.Var.Declaration.IsDeclared)
            .Where(acc => acc.Lifetime == var.Node.Lifetime)
            .GroupBy(acc => acc.Var.Node.Binding.Id)
            .Select(grouping => grouping.First())
            .OrderBy(i => i.Var.Name);

        foreach (var accumulator in accumulators)
        {
            lines.AppendLine($"var {accumulator.Var.Name} = new {accumulator.Var.InstanceType}();");
            accumulator.Var.Declaration.IsDeclared = true;
            accumulator.Var.Declaration.IsCreated = true;
        }

        if (isBlock)
        {
            StartSingleInstanceCheck(parentCtx with { Lines = lines });
        }

        var dependencyVars = new List<Var>();
        var.Declaration.IsCreated = true;

        // Implementation
        if (var.Node.Implementation is {} implementation)
        {
            if (ctx.RootContext.Graph.Graph.TryGetInEdges(var.Node, out var implementationDependencies))
            {
                var vars = implementationDependencies.Select(dependency => varsMap.GetVar(dependency.Injection, dependency.Source, ctx.Path)).ToList();
                foreach (var dependencyVar in SortVarsToBuild(vars))
                {
                    var result = BuildCode(ctx with { Var = dependencyVar });
                    changes = new CodeChanges(changes.Declarations.AddRange(result.Declarations));
                }

                dependencyVars.AddRange(vars);
            }

            var varsWalker = varsWalkerFactory().Initialize(dependencyVars);
            varsWalker.VisitConstructor(Unit.Shared, implementation.Constructor);
            var ctorArgs = varsWalker.GetResult();

            var requiredFields = ImmutableArray.CreateBuilder<(Var RequiredVar, DpField RequiredField)>();
            foreach (var requiredField in implementation.Fields.Where(i => i.Field.IsRequired).OrderBy(i => i.Ordinal ?? int.MaxValue - 1))
            {
                varsWalker.VisitField(Unit.Shared, requiredField, null);
                var dependencyVar = varsWalker.GetResult().Single();
                requiredFields.Add((dependencyVar, requiredField));
            }

            var requiredProperties = ImmutableArray.CreateBuilder<(Var RequiredVar, DpProperty RequiredProperty)>();
            foreach (var requiredProperty in implementation.Properties.Where(i => i.Property.IsRequired || i.Property.SetMethod?.IsInitOnly == true).OrderBy(i => i.Ordinal ?? int.MaxValue))
            {
                varsWalker.VisitProperty(Unit.Shared, requiredProperty, null);
                var dependencyVar = varsWalker.GetResult().Single();
                requiredProperties.Add((dependencyVar, requiredProperty));
            }

            var visits = new List<(Action<CodeContext, string> Run, int? Ordinal)>();
            foreach (var field in implementation.Fields.Where(i => i.Field.IsRequired != true))
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

            var onCreatedStatements = buildTools.OnCreated(ctx, var).ToList();
            var hasOnCreatedStatements = buildTools.OnCreated(ctx, var).Any();
            var hasAlternativeInjections = visits.Count > 0;
            var tempVariableInit =
                ctx.RootContext.IsThreadSafeEnabled
                && var.Node.Lifetime is not Transient and not PerBlock
                && (hasAlternativeInjections || hasOnCreatedStatements);

            var tempVar = var;
            if (tempVariableInit)
            {
                tempVar = var with { NameOverride = var.Declaration.Name + "Temp" };
                lines.AppendLine($"{typeResolver.Resolve(ctx.RootContext.Graph.Source, tempVar.InstanceType)} {tempVar.Name};");
                if (onCreatedStatements.Count > 0)
                {
                    onCreatedStatements = buildTools.OnCreated(ctx, tempVar).ToList();
                }
            }

            var instantiation = CreateInstantiation(ctx, ctorArgs, requiredFields, requiredProperties);
            if (var.Node.Lifetime is not Transient
                || hasAlternativeInjections
                || tempVariableInit
                || hasOnCreatedStatements)
            {
                lines.Append($"{buildTools.GetDeclaration(ctx, var)}{tempVar.Name} = ");
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
            if (var.Node.Arg is {} arg)
            {
                lines.AppendLine(arg.Source.ArgName);
            }
            else
            {
                if (var.Node.Factory is {} factory)
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
                                        var binding = var.Node.Binding;
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
                    var finishLabel = $"{var.Declaration.Name}Finish";
                    var factoryExpression = (LambdaExpressionSyntax)factory.Source.LocalVariableRenamingRewriter.Clone().Rewrite(setup.SemanticModel, setup.Hints.IsFormatCodeEnabled, false, originalLambda);
                    var injections = new List<V2FactoryRewriter.Injection>();
                    var inits = new List<V2FactoryRewriter.Initializer>();
                    var factoryRewriter = new V2FactoryRewriter(arguments, compilations, factory, var, finishLabel, injections, inits, triviaTools, symbolNames);
                    var lambda = factoryRewriter.Rewrite(ctx, factoryExpression);
                    factoryValidatorFactory().Initialize(factory).Visit(lambda);
                    SyntaxNode syntaxNode = lambda.Block is not null ? lambda.Block : SyntaxFactory.ExpressionStatement((ExpressionSyntax)lambda.Body);
                    var hasOverrides = factory.HasOverrides;
                    if (!var.Declaration.IsDeclared && var.HasCycle)
                    {
                        lines.AppendLine($"var {var.Name} = default({buildTools.GetDeclaration(ctx, var, "")});");
                        var.Declaration.IsDeclared = true;
                    }

                    var textLines = new List<TextLine>();
                    var hasOverridesLock = false;
                    if (hasOverrides && ctx.IsLockRequired)
                    {
                        if (!var.Declaration.IsDeclared)
                        {
                            lines.AppendLine($"{buildTools.GetDeclaration(ctx, var)}{var.Name};");
                            var.Declaration.IsDeclared = true;
                        }

                        locks.AddLockStatements(ctx.RootContext.Graph, lines, false);
                        lines.AppendLine(BlockStart);
                        lines.IncIndent();
                        ctx = ctx with { IsLockRequired = false };
                        hasOverridesLock = true;
                    }

                    if (syntaxNode is BlockSyntax curBlock)
                    {
                        if (!var.Declaration.IsDeclared)
                        {
                            lines.Append($"{buildTools.GetDeclaration(ctx, var)}{var.Name};");
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
                        var leadingTrivia = syntaxNode.GetLeadingTrivia().ToFullString().Trim();
                        if (!string.IsNullOrEmpty(leadingTrivia))
                        {
                            lines.AppendLine(leadingTrivia);
                        }

                        if (!var.Declaration.IsDeclared)
                        {
                            lines.Append($"{buildTools.GetDeclaration(ctx, var)}{var.Name} = ");
                            var.Declaration.IsDeclared = true;
                        }
                        else
                        {
                            lines.Append($"{var.Name} = ");
                        }

                        var text = syntaxNode.WithoutTrivia().GetText();
                        textLines.AddRange(text.Lines);
                    }

                    if (ctx.RootContext.Graph.Graph.TryGetInEdges(var.Node, out var dependencies))
                    {
                        foreach (var dependency in dependencies)
                        {
                            var dependencyVar = varsMap.GetVar(dependency.Injection, dependency.Source, ctx.Path);
                            dependencyVars.Add(dependencyVar);
                        }
                    }

                    var injectionArgs = dependencyVars.Where(i => i.Injection.Kind is InjectionKind.FactoryInjection).ToList();
                    var initializationArgs = dependencyVars.Where(i => i.Injection.Kind != InjectionKind.FactoryInjection).ToList();

                    // Replaces injection markers by injection code
                    if (injectionArgs.Count != injections.Count)
                    {
                        throw new CompileErrorException(
                            string.Format(Strings.Error_Template_LifetimeDoesNotSupportCyclicDependencies, var.Node.Lifetime),
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
                    var prefixes = new Stack<string>();
                    foreach (var textLine in textLines)
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
                            using (lines.Indent(indent))
                            {
                                if (hasOverrides)
                                {
                                    BuildOverrides(ctx, factory, resolver.Overrides, lines);
                                }

                                var result = BuildCode(ctx with { Var = argument });
                                changes = new CodeChanges(changes.Declarations.AddRange(result.Declarations));
                                lines.AppendLine($"{(injection.DeclarationRequired ? $"{typeResolver.Resolve(setup, argument.Injection.Type)} " : "")}{injection.VariableName} = {buildTools.OnInjected(ctx, argument)};");
                            }

                            continue;
                        }

                        if (line.Contains(InitializationStatement) && initializers.MoveNext())
                        {
                            var (initialization, initializer) = initializers.Current;
                            if (hasOverrides)
                            {
                                BuildOverrides(ctx, factory, initializer.Overrides, lines);
                            }

                            var results = new List<CodeChanges>();
                            var initializersWalker = initializersWalkerFactory().Ininitialize(
                                i => {
                                    var result = BuildCode(ctx with { Var = i });
                                    results.Add(result);
                                },
                                initialization.VariableName,
                                initializationArgsEnum);

                            foreach (var result in results)
                            {
                                changes = new CodeChanges(changes.Declarations.AddRange(result.Declarations));
                            }

                            initializersWalker.VisitInitializer(ctx, initializer);
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

                        lines.AppendLine(line);
                    }

                    if (factoryRewriter.IsFinishMarkRequired)
                    {
                        lines.AppendLine($"{finishLabel}:;");
                    }

                    lines.AppendLines(buildTools.OnCreated(ctx, var));

                    if (hasOverridesLock)
                    {
                        lines.DecIndent();
                        lines.AppendLine(BlockFinish);
                        locks.AddUnlockStatements(ctx.RootContext.Graph, lines, false);
                    }
                }
                else
                {
                    if (var.Node.Construct is {} construct)
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
                                    if (ctx.RootContext.Graph.Graph.TryGetInEdges(var.Node, out var enumerableDependencies))
                                    {
                                        foreach (var dependency in enumerableDependencies)
                                        {
                                            var dependencyVar = varsMap.GetVar(dependency.Injection, dependency.Source, ctx.Path);
                                            dependencyVars.Add(dependencyVar);
                                            var result = BuildCode(ctx with { Var = dependencyVar });
                                            changes = new CodeChanges(changes.Declarations.AddRange(result.Declarations));
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
                                lines.AppendLine($"{buildTools.GetDeclaration(ctx, var)}{var.Name} = {localMethodName}();");
                                lines.AppendLines(buildTools.OnCreated(ctx, var));
                                break;

                            case MdConstructKind.Array:
                                if (ctx.RootContext.Graph.Graph.TryGetInEdges(var.Node, out var arrayDependencies))
                                {
                                    var vars = arrayDependencies.Select(dependency => varsMap.GetVar(dependency.Injection, dependency.Source, ctx.Path)).ToList();
                                    foreach (var dependencyVar in SortVarsToBuild(vars))
                                    {
                                        var result = BuildCode(ctx with { Var = dependencyVar });
                                        changes = new CodeChanges(changes.Declarations.AddRange(result.Declarations));
                                    }

                                    dependencyVars.AddRange(vars);
                                }

                                var instantiation = $"new {typeResolver.Resolve(ctx.RootContext.Graph.Source, construct.Source.ElementType)}[{dependencyVars.Count.ToString()}] {{ {string.Join(", ", dependencyVars.Select(item => buildTools.OnInjected(ctx, item)))} }}";
                                var onCreated = buildTools.OnCreated(ctx, var).ToList();
                                if (onCreated.Count > 0)
                                {
                                    lines.AppendLine($"{buildTools.GetDeclaration(ctx, var)}{var.Name} = {instantiation};");
                                    lines.AppendLines(onCreated);
                                }
                                else
                                {
                                    var.CodeExpression =  instantiation;
                                }
                                break;

                            case MdConstructKind.Span:
                                if (ctx.RootContext.Graph.Graph.TryGetInEdges(var.Node, out var spanDependencies))
                                {
                                    var vars = spanDependencies.Select(dependency => varsMap.GetVar(dependency.Injection, dependency.Source, ctx.Path)).ToList();
                                    foreach (var dependencyVar in SortVarsToBuild(vars))
                                    {
                                        var result = BuildCode(ctx with { Var = dependencyVar });
                                        changes = new CodeChanges(changes.Declarations.AddRange(result.Declarations));
                                    }

                                    dependencyVars.AddRange(vars);
                                }

                                var createArray = $"{typeResolver.Resolve(ctx.RootContext.Graph.Source, construct.Source.ElementType)}[{dependencyVars.Count.ToString()}] {{ {string.Join(", ", dependencyVars.Select(item => buildTools.OnInjected(ctx, item)))} }}";

                                var isStackalloc =
                                    construct.Source.ElementType.IsValueType
                                    && compilations.GetLanguageVersion(construct.Binding.SemanticModel.Compilation) >= LanguageVersion.CSharp7_3;

                                var createInstance = isStackalloc ? $"stackalloc {createArray}" : $"new {Names.SystemNamespace}Span<{typeResolver.Resolve(ctx.RootContext.Graph.Source, construct.Source.ElementType)}>(new {createArray})";
                                lines.AppendLine($"{buildTools.GetDeclaration(ctx, var)}{var.Name} = {createInstance};");
                                lines.AppendLines(buildTools.OnCreated(ctx, var));
                                break;

                            case MdConstructKind.Composition:
                                lines.AppendLine($"{buildTools.GetDeclaration(ctx, var)}{var.Name} = this;");
                                break;

                            case MdConstructKind.OnCannotResolve:
                                lines.AppendLine($"{buildTools.GetDeclaration(ctx, var)}{var.Name} = {Names.OnCannotResolve}<{var.ContractType}>({var.Injection.Tag.ValueToString()}, {var.Node.Lifetime.ValueToString()});");
                                break;

                            case MdConstructKind.ExplicitDefaultValue:
                                lines.AppendLine($"{buildTools.GetDeclaration(ctx, var)}{var.Name} = {construct.Source.ExplicitDefaultValue.ValueToString()};");
                                break;

                            case MdConstructKind.Accumulator:
                                lines.AppendLine($"{buildTools.GetDeclaration(ctx, var)}{var.Name} = new {construct.Source.Type}();");
                                break;

                            case MdConstructKind.Override:
                                break;

                            case MdConstructKind.None:
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
            }
        }

        if (isBlock)
        {
            FinishSingleInstanceCheck(ctx with { IsLockRequired = parentCtx.IsLockRequired });
        }

        parentCtx.Lines.AppendLines(lines.Lines);
        if (isLazy || isBlock)
        {
            changes = varsMap.Reset(changes, isLazy, ctx.Path);
        }

        var.Declaration.IsDeclared = true;
        return new CodeChanges(Declarations: changes.Declarations.Add(var.Declaration));
    }

    private static IEnumerable<Var> SortVarsToBuild(List<Var> vars) => vars.OrderBy(GetVarPriority);

    private static int GetVarPriority(Var var)
    {
        var node = var.Node;
        if (node.Arg is not null)
        {
            return 1;
        }

        if (node.Implementation is not null)
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

        if (node.Factory is {} factory)
        {
            return 30 + factory.Resolvers.Length + factory.Initializers.Length * 100 + (factory.HasOverrides ? 1000 : 0);
        }

        return int.MaxValue;
    }

    private static bool IsBlock(DependencyNode node) => node.Lifetime is Singleton or Scoped or PerResolve;

    private bool IsLazy(DependencyNode node) => nodeInfo.IsLazy(node);

    private void StartSingleInstanceCheck(CodeContext ctx)
    {
        var isLockRequired = ctx.IsLockRequired;
        var var = ctx.Var;
        var lines = ctx.Lines;
        var compilation = var.Node.Binding.SemanticModel.Compilation;
        var checkExpression = var.InstanceType.IsValueType
            ? $"!{var.Name}Created"
            : buildTools.NullCheck(compilation, var.Name);

        if (isLockRequired)
        {
            lines.AppendLine($"if ({checkExpression})");
            lines.AppendLine(BlockStart);
            lines.IncIndent();
            locks.AddLockStatements(ctx.RootContext.Graph, lines, false);
            lines.AppendLine(BlockStart);
            lines.IncIndent();
        }

        lines.AppendLine($"if ({checkExpression})");
        lines.AppendLine(BlockStart);
        lines.IncIndent();
    }

    private void FinishSingleInstanceCheck(CodeContext ctx)
    {
        var var = ctx.Var;
        var lines = ctx.Lines;
        if (var.Node.Lifetime is Singleton or Scoped && nodeInfo.IsDisposableAny(var.Node))
        {
            var parent = "";
            if (var.Node.Lifetime == Singleton)
            {
                parent = $"{Names.RootFieldName}.";
            }

            lines.AppendLine($"{parent}{Names.DisposablesFieldName}[{parent}{Names.DisposeIndexFieldName}++] = {var.Name};");
        }

        if (var.InstanceType.IsValueType)
        {
            if (var.Node.Lifetime is not Transient and not PerBlock && ctx.RootContext.IsThreadSafeEnabled)
            {
                lines.AppendLine($"{Names.SystemNamespace}Threading.Thread.MemoryBarrier();");
            }

            lines.AppendLine($"{var.Name}Created = true;");
        }

        if (ctx.IsLockRequired)
        {
            lines.DecIndent();
            lines.AppendLine(BlockFinish);
            lines.DecIndent();
            lines.AppendLine(BlockFinish);
        }

        lines.DecIndent();
        lines.AppendLine(BlockFinish);
        lines.AppendLine();
    }

    private string CreateInstantiation(
        CodeContext ctx,
        IReadOnlyCollection<Var> ctorArgs,
        IReadOnlyCollection<(Var RequiredVariable, DpField RequiredField)> requiredFields,
        IReadOnlyCollection<(Var RequiredVariable, DpProperty RequiredProperty)> requiredProperties)
    {
        var var = ctx.Var;
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

    private static bool IsCycle(CodeContext ctx)
    {
        var var = ctx.Var;
        var graph = ctx.RootContext.Graph.Graph;
        var processed = new HashSet<DependencyNode>();
        var nodes = new Stack<DependencyNode>();
        nodes.Push(var.Node);
        while (nodes.TryPop(out var node))
        {
            if (!processed.Add(node))
            {
                continue;
            }

            if (graph.TryGetInEdges(node, out var dependencies))
            {
                foreach (var dependency in dependencies)
                {
                    var source = dependency.Source;
                    if (source.Binding.Id == var.Node.Binding.Id)
                    {
                        return true;
                    }

                    nodes.Push(source);
                }
            }
        }

        return false;
    }

    private IEnumerable<Accumulator> GetAccumulators(
        IVarsMap varsMap,
        IGraph<DependencyNode, Dependency> graph,
        DependencyNode targetNode,
        DeclarationPath path)
    {
        var processed = new HashSet<DependencyNode>();
        var nodes = new Stack<DependencyNode>();
        nodes.Push(targetNode);
        while (nodes.TryPop(out var node))
        {
            if (!processed.Add(node))
            {
                continue;
            }

            if (graph.TryGetInEdges(node, out var dependencies))
            {
                foreach (var dependency in dependencies)
                {
                    var source = dependency.Source;
                    if (IsLazy(source))
                    {
                        continue;
                    }

                    if (source.Construct is {} construct && construct.Source.Kind == MdConstructKind.Accumulator && construct.Source.State is IEnumerable<MdAccumulator> mdAccumulators)
                    {
                        foreach (var acc in mdAccumulators)
                        {
                            var var = varsMap.GetVar(dependency.Injection, source, path);
                            yield return new Accumulator(var, acc.Type, acc.Lifetime);
                        }

                        continue;
                    }

                    nodes.Push(source);
                }
            }
        }
    }

    private void BuildOverrides(CodeContext ctx, DpFactory factory, ImmutableArray<DpOverride> overrides, LinesBuilder lines)
    {
        foreach (var @override in overrides.OrderBy(i => i.Source.Position).Select(i => factory.ResolveOverride(i)))
        {
            var name = variableNameProvider.GetOverrideVariableName(@override.Source);
            var isDeclared = !ctx.Overrides.Add(name);
            lines.AppendLine($"{(isDeclared ? "" : $"{typeResolver.Resolve(ctx.RootContext.Graph.Source, @override.Source.ContractType)} ")}{name} = {@override.Source.ValueExpression};");
            overridesRegistry.Register(ctx.RootContext.Root, @override);
        }
    }
}