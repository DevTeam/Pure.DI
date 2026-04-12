// ReSharper disable ConvertIfStatementToConditionalTernaryExpression
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
    CancellationToken cancellationToken)
    : IBuilder<DependencyGraph, CompositionCode>
{
    public CompositionCode Build(DependencyGraph graph)
    {
        var roots = new List<Root>();
        var varsMap = varsMapFactory();
        var classArgs = new List<VarDeclaration>();
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        var isThreadSafe = false;
        var isAnyConstructorEnabled = constructors.IsEnabled(graph);
        foreach (var root in graph.Roots)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var typeDescription = typeResolver.Resolve(graph.Source, root.Injection.Type);
            var processedRoot = root with { TypeDescription = typeDescription };
            if (typeDescription.TypeArgs.Count > 0)
            {
                processedRoot = processedRoot with { Kind = processedRoot.Kind & ~RootKinds.Light };
            }

            IEnumerable<VarDeclaration> args;
            var lines = new Lines();
            using var rootToken = varsMap.Root(lines);
            if (root.Source.Kind.HasFlag(RootKinds.Light) && typeDescription.TypeArgs.Count == 0)
            {
                var rootArgsContext = new RootArgsContext(varsMap, new List<VarDeclaration>());
                graphArgsWalker.Walk(rootArgsContext, graph, root.Node, rootArgsVisitor, cancellationToken);
                args = rootArgsContext.Args;
            }
            else
            {
                var ctx = new RootContext(graph, root, varsMap, lines);
                if (isAnyConstructorEnabled)
                {
                    var statisticsContext = new RootStatisticsContext();
                    graphStatisticsWalker.Walk(statisticsContext, graph, root.Node, rootStatisticsVisitor, cancellationToken);
                    if (statisticsContext.GetNodeCountByLifetime(Lifetime.Singleton) > 0 )
                    {
                        lines.Append($"var {Names.RootVarName} = {Names.RootFieldName} ?? this;");
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
                }

                lines.AppendLine($"return {rootVarInjection.Var.CodeExpression};");
                foreach (var localFunction in varsMap.Vars.Select(i => i.LocalFunction).Where(i => i.Count > 0))
                {
                    lines.AppendLine();
                    lines.AppendLines(localFunction);
                }

                processedRoot = processedRoot with { Lines = ctx.Lines };
                args = varsMap.Declarations.Where(i => i.Node.Arg is not null);
            }

            var currentArgs = varDeclarationTools.Sort(args).ToList();

            var currentClassArgs = currentArgs.GetArgsOfKind(ArgKind.Composition)
                .Where(arg => arg.Node.Arg is not { Source.IsSetupContext: true })
                .Where(arg => bindingsRegistry.IsRegistered(graph.Source, arg.Node.BindingId));

            classArgs.AddRange(currentClassArgs);

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
            .Where(i => i.Node.ActualLifetime is Lifetime.Singleton or Lifetime.Scoped)
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
        var setupContextArgsToCopy = setupContextArgs
            .Where(arg => arg.Kind != SetupContextKind.RootArgument)
            .ToImmutableArray();

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
        var isLockRequired = isThreadSafe || HasRootOverrides(graph);
        var requiresParentScope = singletons.Length > 0
                                  || classArgsToStore.Length > 0
                                  || setupContextArgsToCopy.Length > 0
                                  || setupContextMembersToCopy.Length > 0
                                  || isLockRequired
                                  || totalDisposablesCount > 0;
        var scopeFactoryName = graph.Source.Hints.ScopeFactoryName;
        var isFactoryMethod = requiresParentScope && !string.IsNullOrWhiteSpace(scopeFactoryName);
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
            scopeFactoryName,
            isFactoryMethod,
            requiresParentScope,
            isLockRequired);

        var diagram = classDiagramBuilder.Build(composition);
        return composition with { Diagram = diagram };
    }

    private bool HasRootOverrides(DependencyGraph graph) =>
        graph.Roots.Any(root => overridesRegistry.GetOverrides(root).Any());

    private static ImmutableArray<string> GetSetupContextMembersToCopy(ImmutableArray<SetupContextMembers> setupContextMembers)
    {
        var memberNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var setupContext in setupContextMembers)
        {
            foreach (var member in setupContext.Members)
            {
                switch (member)
                {
                    case FieldDeclarationSyntax { Declaration: { } declaration }:
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
