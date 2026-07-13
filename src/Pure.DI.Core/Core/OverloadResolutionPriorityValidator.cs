// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable LoopCanBeConvertedToQuery
namespace Pure.DI.Core;

sealed class OverloadResolutionPriorityValidator(
    ILogger logger,
    ISemantic semantic,
    IOverloadResolutionPriority overloadResolutionPriority)
    : IValidator<DependencyGraph>
{
    public bool Validate(DependencyGraph dependencyGraph)
    {
        if (!dependencyGraph.IsResolved)
        {
            return false;
        }

        var compilation = dependencyGraph.Source.SemanticModel.Compilation;
        var reported = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
        foreach (var method in dependencyGraph.Graph.Vertices
                     .SelectMany(node => node.Implementation?.Methods ?? ImmutableArray<DpMethod>.Empty)
                     .Select(i => i.Method))
        {
            if (!reported.Add(method) || FindHigherPriorityOverload(compilation, method) is not {} overload)
            {
                continue;
            }

            logger.CompileWarning(
                LogMessage.From(
                    nameof(Strings.Description_WarningInjectionMethodOverloadResolutionPriority),
                    Strings.Description_WarningInjectionMethodOverloadResolutionPriority),
                method.Locations.AddRange(overload.Locations),
                LogId.WarningInjectionMethodOverloadResolutionPriority);
        }

        return true;
    }

    private IMethodSymbol? FindHigherPriorityOverload(Compilation compilation, IMethodSymbol method)
    {
        var priority = overloadResolutionPriority.Get(compilation, method);
        foreach (var candidate in method.ContainingType.GetMembers(method.Name).OfType<IMethodSymbol>())
        {
            if (SymbolEqualityComparer.Default.Equals(method, candidate)
                || candidate.IsStatic
                || candidate.MethodKind != MethodKind.Ordinary
                || !semantic.IsAccessible(candidate)
                || overloadResolutionPriority.Get(compilation, candidate) <= priority
                || !IsApplicable(compilation, method.Parameters, candidate.Parameters))
            {
                continue;
            }

            return candidate;
        }

        return null;
    }

    private static bool IsApplicable(
        Compilation compilation,
        ImmutableArray<IParameterSymbol> source,
        ImmutableArray<IParameterSymbol> target)
    {
        if (source.Length != target.Length)
        {
            return false;
        }

        for (var i = 0; i < source.Length; i++)
        {
            if (source[i].RefKind != target[i].RefKind
                || !compilation.ClassifyConversion(source[i].Type, target[i].Type).IsImplicit)
            {
                return false;
            }
        }

        return true;
    }
}
