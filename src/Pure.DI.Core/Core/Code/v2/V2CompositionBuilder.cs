// ReSharper disable ConvertIfStatementToConditionalTernaryExpression
namespace Pure.DI.Core.Code.v2;

class V2CompositionBuilder(
    ITypeResolver typeResolver,
    Func<IVarsMap> varsMapFactory,
    IBuilder<RootContext, Var> rootBuilder,
    INodeInfo nodeInfo,
    IBuilder<CompositionCode, LinesBuilder> classDiagramBuilder,
    CancellationToken cancellationToken)
    : IBuilder<DependencyGraph, CompositionCode>
{
    public CompositionCode Build(DependencyGraph graph)
    {
        var roots = new List<Root>();
        var varsMap = varsMapFactory();
        var classArgs = new HashSet<VarDeclaration>();
        var isThreadSafe = false;
        var isThreadSafeEnabled = graph.Source.Hints.IsThreadSafeEnabled;
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var root in graph.Roots)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            // Remove all but the singletons
            varsMap.Remove(
                varsMap.GetBindings()
                    .Where(binding =>
                        (binding.Lifetime?.Value ?? Lifetime.Transient) is not (Lifetime.Singleton or Lifetime.Scoped)));

            var ctx = new RootContext(graph, root, varsMap, new LinesBuilder());
            var rootVar = rootBuilder.Build(ctx);
            ctx.Lines.AppendLine($"return {rootVar.CodeExpression};");

            var lines = new LinesBuilder();
            foreach (var perResolve in varsMap.GetPerResolves())
            {
                lines.AppendLine($"var {perResolve.Name} = default({typeResolver.Resolve(graph.Source, perResolve.InstanceType)});");
                if (perResolve.InstanceType.IsValueType)
                {
                    lines.AppendLine($"var {perResolve.Name}Created = false;");
                }
            }

            lines.AppendLines(ctx.Lines.Lines);

            var args = varsMap.GetArgs().ToList();
            var processedRoot = root with
            {
                Lines = lines.Lines.ToImmutableArray(),
                TypeDescription = typeResolver.Resolve(graph.Source, root.Injection.Type),
                RootArgs = args.GetArgsOfKind(ArgKind.Root).ToImmutableArray()
            };

            foreach (var classArg in args.GetArgsOfKind(ArgKind.Class))
            {
                classArgs.Add(classArg);
            }

            var typeDescription = typeResolver.Resolve(graph.Source, processedRoot.Injection.Type);
            var isMethod = (processedRoot.Kind & RootKinds.Method) == RootKinds.Method
                           || processedRoot.RootArgs.Length > 0
                           || typeDescription.TypeArgs.Count > 0;

            processedRoot = processedRoot with
            {
                TypeDescription = typeDescription,
                IsMethod = isMethod
            };

            roots.Add(processedRoot);
            isThreadSafe |= isThreadSafeEnabled
                            && varsMap.GetBindings().Any(binding =>
                                binding.Lifetime?.Value is Lifetime.Singleton or Lifetime.Scoped or Lifetime.PerResolve
                                || binding.Construct is { Kind: MdConstructKind.Accumulator });
        }

        var publicRoots = roots
            .OrderByDescending(root => root.IsPublic)
            .ThenBy(root => root.Node.Binding.Id)
            .ThenBy(root => root.DisplayName)
            .ToImmutableArray();

        var singletons = varsMap.GetSingletons().ToImmutableArray();
        var totalDisposables = singletons.Where(i => nodeInfo.IsDisposableAny(i.Node)).ToList();
        var disposables = singletons.Where(i => nodeInfo.IsDisposable(i.Node)).ToList();
        var asyncDisposables = singletons.Where(i => nodeInfo.IsAsyncDisposable(i.Node)).ToList();
        var composition = new CompositionCode(
            graph,
            new LinesBuilder(),
            ImmutableArray<Variable>.Empty,
            ImmutableArray<Variable>.Empty,
            publicRoots,
            totalDisposables.Count,
            disposables.Count,
            asyncDisposables.Count,
            totalDisposables.Count(i => i.Node.Lifetime == Lifetime.Scoped),
            isThreadSafe,
            ImmutableArray<Line>.Empty,
            singletons,
            varsMap.Sort(classArgs).ToImmutableArray());

        var diagram = classDiagramBuilder.Build(composition);
        return composition with { Diagram = diagram.Lines.ToImmutableArray() };
    }
}