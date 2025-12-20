// ReSharper disable ConvertIfStatementToConditionalTernaryExpression
namespace Pure.DI.Core.Code;

class CompositionBuilder(
    ITypeResolver typeResolver,
    Func<IVarsMap> varsMapFactory,
    Func<IBuilder<RootContext, VarInjection>> rootBuilder,
    INodeTools nodeTools,
    IVarDeclarationTools varDeclarationTools,
    IBuilder<CompositionCode, Lines> classDiagramBuilder,
    IOverridesRegistry overridesRegistry,
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

            var lines = new Lines();
            using var rootToken = varsMap.Root(lines);
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

            var args = varDeclarationTools.Sort(varsMap.Declarations.Where(i => i.Node.Arg is not null)).ToList();
            var processedRoot = root with
            {
                Lines = ctx.Lines,
                TypeDescription = typeResolver.Resolve(graph.Source, root.Injection.Type),
                RootArgs = args.GetArgsOfKind(ArgKind.Root).ToImmutableArray()
            };

            classArgs.AddRange(args.GetArgsOfKind(ArgKind.Composition));
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
        }

        var singletons = varsMap.Declarations.Where(i => i.Node.ActualLifetime is Lifetime.Singleton or Lifetime.Scoped).ToImmutableArray();
        var publicRoots = roots
            .OrderByDescending(root => root.IsPublic)
            .ThenBy(root => root.Node.Binding.Id)
            .ThenBy(root => root.DisplayName)
            .ToImmutableArray();

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
            varDeclarationTools.Sort(classArgs).Distinct().ToImmutableArray());

        var diagram = classDiagramBuilder.Build(composition);
        return composition with { Diagram = diagram };
    }
}