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
    IGraphWalker<RootArgsContext, ImmutableArray<Dependency>> graphWalker,
    IGraphVisitor<RootArgsContext, ImmutableArray<Dependency>> rootArgsVisitor,
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
                graphWalker.Walk(rootArgsContext, graph, root.Node, rootArgsVisitor, cancellationToken);
                args = rootArgsContext.Args;
            }
            else
            {
                var ctx = new RootContext(graph, root, varsMap, lines);
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

        var singletons = varsMap.Declarations.Where(i => i.Node.ActualLifetime is Lifetime.Singleton or Lifetime.Scoped).ToImmutableArray();
        var publicRoots = roots
            .OrderByDescending(root => root.IsPublic)
            .ThenBy(root => root.Node.Binding.Id)
            .ThenBy(root => root.DisplayName)
            .ToImmutableArray();

        var setupContextArgs = graph.Source.Bindings
            .Select(binding => binding.Arg)
            .Where(arg => arg is { IsSetupContext: true })
            .Select(arg => arg.GetValueOrDefault())
            .Where(arg => arg.SetupContextKind != SetupContextKind.RootArgument && arg.SetupContextKind != SetupContextKind.Members)
            .Select(arg => new SetupContextArg(arg.Type, arg.ArgName, arg.SetupContextKind))
            .GroupBy(arg => arg.Name)
            .Select(group => group.First())
            .ToImmutableArray();
        var setupContextMembers = graph.Source.SetupContextMembers;

        var totalDisposables = singletons.Where(i => nodeTools.IsDisposableAny(i.Node.Node)).ToList();
        var disposables = singletons.Where(i => nodeTools.IsDisposable(i.Node.Node)).ToList();
        var asyncDisposables = singletons.Where(i => nodeTools.IsAsyncDisposable(i.Node.Node)).ToList();
        var composition = new CompositionCode(
            graph,
            new Lines(),
            publicRoots,
            totalDisposables.Count,
            disposables.Count,
            asyncDisposables.Count,
            totalDisposables.Count(i => i.Node.ActualLifetime == Lifetime.Scoped),
            isThreadSafe,
            new Lines(),
            singletons,
            varDeclarationTools.Sort(classArgs).Distinct().ToImmutableArray(),
            setupContextArgs,
            setupContextMembers);

        var diagram = classDiagramBuilder.Build(composition);
        return composition with { Diagram = diagram };
    }
}
