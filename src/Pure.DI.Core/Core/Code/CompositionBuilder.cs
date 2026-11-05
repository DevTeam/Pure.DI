// ReSharper disable ConvertIfStatementToConditionalTernaryExpression
// ReSharper disable UseCollectionExpression
namespace Pure.DI.Core.Code;

class CompositionBuilder(
    ITypeResolver typeResolver,
    Func<IVarsMap> varsMapFactory,
    Func<IFastBuilder<RootContext, VarInjection>> rootBuilder,
    INodeTools nodeTools,
    IVarDeclarationTools varDeclarationTools,
    IBuilder<CompositionCode, Lines> classDiagramBuilder,
    IOverridesRegistry overridesRegistry,
    IRegistry<int> bindingsRegistry,
    IGraphWalker<RootArgsContext, ImmutableArray<Dependency>> graphArgsWalker,
    IGraphVisitor<RootArgsContext, ImmutableArray<Dependency>> rootArgsVisitor,
    IGraphWalker<RootStatisticsContext, DependencyNode> graphStatisticsWalker,
    IGraphVisitor<RootStatisticsContext, DependencyNode> rootStatisticsVisitor,
    IConstructors constructors,
    ISharedSingletons sharedSingletons,
    IRootUseSiteCounter rootUseSiteCounter,
    ILogger logger,
    ILocationProvider locationProvider,
    CancellationToken cancellationToken)
    : IBuilder<DependencyGraph, CompositionCode>
{
    private const int MinTooLargeCodeLines = 2000;
    private const int MaxLinesPerDependency = 50;

    public CompositionCode Build(DependencyGraph graph)
    {
        var roots = new List<Root>();
        var varsMap = varsMapFactory();
        var classArgs = new List<VarDeclaration>();
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        var isThreadSafe = false;
        var isInstanceLockUsed = false;
        var isAnyConstructorEnabled = constructors.IsEnabled(graph);
        foreach (var root in graph.Roots)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var typeDescription = typeResolver.Resolve(graph.Source, root.Injection.Type);
            var processedRoot = root with { TypeDescription = typeDescription };
            if (root.Injection.Type is INamedTypeSymbol { IsGenericType: true })
            {
                processedRoot = processedRoot with { Kind = processedRoot.Kind & ~RootKinds.Light };
            }

            IEnumerable<VarDeclaration> args;
            var lines = new Lines();
            using var rootToken = varsMap.Root(lines);
            if (processedRoot.Kind.HasFlag(RootKinds.Light))
            {
                var rootArgsContext = new RootArgsContext(varsMap, new List<VarDeclaration>());
                graphArgsWalker.Walk(rootArgsContext, graph, root.Node, rootArgsVisitor, cancellationToken);
                args = rootArgsContext.Args;
            }
            else
            {
                var useSites = rootUseSiteCounter.Analyze(graph, root.Node);
                var ctx = new RootContext(graph, root, varsMap, lines, useSites);
                if (isAnyConstructorEnabled)
                {
                    var statisticsContext = new RootStatisticsContext();
                    graphStatisticsWalker.Walk(statisticsContext, graph, root.Node, rootStatisticsVisitor, cancellationToken);
                    if (statisticsContext.GetNodeCountByLifetime(Lifetime.Singleton) > 0 )
                    {
                        if (string.IsNullOrWhiteSpace(graph.Source.Hints.ScopeMethodName))
                        {
                            lines.Append($"var {Names.SingletonsVarName} = {Names.SingletonsFieldName};");
                        }
                        else
                        {
                            // The root composition of a scope method has no constructor that creates the shared singletons.
                            lines.AppendLine($"var {Names.RootVarName} = {Names.RootFieldName} ?? this;");
                            lines.Append($"var {Names.SingletonsVarName} = {sharedSingletons.GetOrCreateExpression("")};");
                        }
                    }
                }

                var rootVarInjection = rootBuilder().Build(ctx);
                var isThreadSafeRoot = ctx.LockIsInUse || graph.Source.Hints.IsThreadSafeEnabled && varsMap.IsThreadSafe;
                if (root.IsStatic)
                {
                    if (isThreadSafeRoot || overridesRegistry.GetOverrides(root).Any())
                    {
                        // resolveLock local field
                        var newLines = new Lines();
                        newLines.AppendLine(new Line(int.MinValue, "#if NET9_0_OR_GREATER"));
                        newLines.AppendLine($"var {Names.PerResolveLockFieldName} = new {Names.LockTypeName}();");
                        newLines.AppendLine(new Line(int.MinValue, "#else"));
                        newLines.AppendLine($"var {Names.PerResolveLockFieldName} = new {Names.ObjectTypeName}();");
                        newLines.AppendLine(new Line(int.MinValue, "#endif"));
                        newLines.AppendLines(ctx.Lines);
                        lines = newLines;
                        ctx = ctx with { Lines = lines };
                    }
                }
                else
                {
                    isThreadSafe |= isThreadSafeRoot;
                    isInstanceLockUsed |= ctx.LockIsInUse;
                }

                if (!ctx.ReturnWasAdded)
                {
                    lines.AppendLine($"return {rootVarInjection.Var.CodeExpression};");
                }
                foreach (var localFunction in ctx.LocalFunctionScope.Functions)
                {
                    lines.AppendLine();
                    lines.AppendLines(localFunction);
                }

                WarnIfCodeIsTooLarge(processedRoot, ctx.Lines.Count, GetDependenciesCount(graph, root.Node));
                processedRoot = processedRoot with { Lines = ctx.Lines };
                args = varsMap.Declarations.Where(i => i.Node.Arg is not null);
            }

            var currentArgs = varDeclarationTools.Sort(args).ToList();

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var currentClassArg in currentArgs.GetArgsOfKind(ArgKind.Composition))
            {
                if (currentClassArg.Node.Arg is { Source.IsSetupContext: true }
                    || !bindingsRegistry.IsRegistered(graph.Source, currentClassArg.Node.BindingId))
                {
                    continue;
                }

                classArgs.Add(currentClassArg);
            }

            var currentRootArgs = currentArgs.GetArgsOfKind(ArgKind.Root).ToImmutableArray();

            var isMethod = processedRoot.Source.IsBuilder
                           || (processedRoot.Kind & RootKinds.Method) == RootKinds.Method
                           || currentRootArgs.Length > 0
                           || typeDescription.TypeArgs.Count > 0;

            processedRoot = processedRoot with
            {
                RootArgs = currentRootArgs.ToImmutableArray(),
                IsMethod = isMethod
                // , Kind = processedRoot.Kind & ~RootKinds.Light
            };

            roots.Add(processedRoot);
        }

        var singletons = varsMap.Declarations
            .Where(i => i.Node.ActualLifetime is Lifetime.Singleton or Lifetime.Scoped && i.Node.Arg is null)
            .ToImmutableArray();
        var publicRoots = roots
            .OrderByDescending(root => root.IsPublic)
            .ThenBy(root => root.Node.Binding.Id)
            .ThenBy(root => root.DisplayName)
            .ToImmutableArray();

        var setupContextArgs = new List<SetupContextArg>();
        var setupContextNames = new HashSet<string>(StringComparer.Ordinal);
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var binding in graph.Source.Bindings)
        {
            var arg = binding.Arg;
            if (arg is not { IsSetupContext: true })
            {
                continue;
            }

            var setupArg = arg.GetValueOrDefault();
            if (setupArg.SetupContextKind is SetupContextKind.RootArgument or SetupContextKind.Members)
            {
                continue;
            }

            if (setupContextNames.Add(setupArg.ArgName))
            {
                setupContextArgs.Add(new SetupContextArg(setupArg.Type, setupArg.ArgName, setupArg.SetupContextKind));
            }
        }
        var setupContextMembers = graph.Source.SetupContextMembers;
        var setupContextMembersToCopy = GetSetupContextMembersToCopy(setupContextMembers);
        var setupContextArgsToCopyBuilder = ImmutableArray.CreateBuilder<SetupContextArg>();
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var setupContextArg in setupContextArgs)
        {
            if (setupContextArg.Kind != SetupContextKind.RootArgument)
            {
                setupContextArgsToCopyBuilder.Add(setupContextArg);
            }
        }

        var setupContextArgsToCopy = setupContextArgsToCopyBuilder.ToImmutable();

        var totalDisposablesCount = 0;
        var disposablesCount = 0;
        var asyncDisposablesCount = 0;
        var disposablesScopedCount = 0;
        foreach (var singleton in singletons)
        {
            var node = singleton.Node.Node;
            if (nodeTools.IsDisposableAny(node))
            {
                totalDisposablesCount++;
                if (singleton.Node.ActualLifetime == Lifetime.Scoped)
                {
                    disposablesScopedCount++;
                }
            }

            if (nodeTools.IsDisposable(node))
            {
                disposablesCount++;
            }

            if (nodeTools.IsAsyncDisposable(node))
            {
                asyncDisposablesCount++;
            }
        }

        var classArgsToStore = varDeclarationTools.Sort(classArgs).Distinct().ToImmutableArray();
        var isLockRequired = isInstanceLockUsed;
        var requiresParentScope = singletons.Length > 0
                                  || classArgsToStore.Length > 0
                                  || setupContextArgsToCopy.Length > 0
                                  || setupContextMembersToCopy.Length > 0
                                  || isLockRequired
                                  || totalDisposablesCount > 0;
        var scopeMethodName = graph.Source.Hints.ScopeMethodName;
        var isScopeMethod = requiresParentScope && !string.IsNullOrWhiteSpace(scopeMethodName);
        var composition = new CompositionCode(
            graph,
            new Lines(),
            publicRoots,
            totalDisposablesCount,
            disposablesCount,
            asyncDisposablesCount,
            disposablesScopedCount,
            isThreadSafe,
            new Lines(),
            singletons,
            classArgsToStore,
            setupContextArgs.ToImmutableArray(),
            setupContextMembers,
            setupContextArgsToCopy,
            setupContextMembersToCopy,
            scopeMethodName,
            isScopeMethod,
            isLockRequired);

        var diagram = classDiagramBuilder.Build(composition);
        return composition with { Diagram = diagram };
    }

    // A graph that is created in place on many dependency paths makes code that grows with the number of paths,
    // which a user does not see until the build becomes slow, see #172 and #178.
    private void WarnIfCodeIsTooLarge(Root root, int linesCount, int dependenciesCount)
    {
        if (linesCount < MinTooLargeCodeLines || linesCount <= dependenciesCount * MaxLinesPerDependency)
        {
            return;
        }

        logger.CompileWarning(
            LogMessage.Format(
                nameof(Strings.Warning_Template_RootCodeIsTooLarge),
                Strings.Warning_Template_RootCodeIsTooLarge,
                root.IsPublic ? $"{root.TypeDescription} {root.DisplayName}" : root.TypeDescription.ToString(),
                linesCount,
                dependenciesCount),
            ImmutableArray.Create(locationProvider.GetLocation(root.Source.Source)),
            LogId.WarningRootCodeIsTooLarge);
    }

    // The copies of a node made for different override scopes, and the values that override an injection, are counted once,
    // so a graph that grows with the number of combinations of overridden delegate arguments is compared
    // with the bindings it is made of rather than with its nodes, see #184.
    private static int GetDependenciesCount(DependencyGraph graph, DependencyNode root)
    {
        var dependencies = new HashSet<string> { GetDependencyKey(root) };
        var visited = new HashSet<int> { root.BindingId };
        var stack = new Stack<DependencyNode>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            if (!graph.Graph.TryGetInEdges(stack.Pop(), out var edges))
            {
                continue;
            }

            foreach (var edge in edges)
            {
                var source = edge.Source;
                if (visited.Add(source.BindingId))
                {
                    dependencies.Add(GetDependencyKey(source));
                    stack.Push(source);
                }
            }
        }

        return dependencies.Count;
    }

    // A copy keeps the id of the binding it is made from first in OriginalIds, as a generic binding does for its constructed types.
    private static string GetDependencyKey(DependencyNode node)
    {
        var binding = node.Binding;
        var originalId = node.Construct is { Source.Kind: MdConstructKind.Override }
            ? -1
            : binding.OriginalIds.IsDefaultOrEmpty ? binding.Id : binding.OriginalIds[0];
        return $"{originalId}:{node.Type}";
    }

    private static ImmutableArray<string> GetSetupContextMembersToCopy(ImmutableArray<SetupContextMembers> setupContextMembers)
    {
        var memberNames = new HashSet<string>(StringComparer.Ordinal);
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var setupContext in setupContextMembers)
        {
            foreach (var member in setupContext.Members)
            {
                switch (member)
                {
                    case FieldDeclarationSyntax { Declaration: { } declaration }:
                        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
                        foreach (var variable in declaration.Variables)
                        {
                            if (!variable.Identifier.IsKind(SyntaxKind.None))
                            {
                                memberNames.Add(variable.Identifier.ValueText);
                            }
                        }

                        break;

                    case PropertyDeclarationSyntax property when IsPropertyAssignable(property):
                        if (!property.Identifier.IsKind(SyntaxKind.None))
                        {
                            memberNames.Add(property.Identifier.ValueText);
                        }

                        break;
                }
            }
        }

        return memberNames.ToImmutableArray();
    }

    private static bool IsPropertyAssignable(PropertyDeclarationSyntax property)
    {
        if (property.ExpressionBody is not null || property.AccessorList is not { } accessorList)
        {
            return false;
        }

        return accessorList.Accessors.Any(accessor =>
               accessor.Kind() is SyntaxKind.SetAccessorDeclaration or SyntaxKind.InitAccessorDeclaration)
               || accessorList.Accessors.All(accessor => accessor.Body is null && accessor.ExpressionBody is null);
    }
}
