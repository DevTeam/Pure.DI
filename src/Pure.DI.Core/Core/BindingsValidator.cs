// ReSharper disable ClassNeverInstantiated.Global
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.Core;

sealed class BindingsValidator(
    ILogger logger,
    IRegistry<int> bindingsRegistry,
    ILocationProvider locationProvider)
    : IValidator<DependencyGraph>
{
    public bool Validate(DependencyGraph graph)
    {
        var overriddenInjections = GetOverriddenInjections(graph);
        foreach (var binding in graph.Source.Bindings.Where(i => i.SourceSetup.Kind == CompositionKind.Public && i.Contracts.All(c => c.Kind == ContractKind.Explicit)))
        {
            // ReSharper disable once InvertIf
            if (!GetIds(binding).Any(id => bindingsRegistry.IsRegistered(graph.Source, id)))
            {
                if (IsOverridden(binding, overriddenInjections))
                {
                    continue;
                }

                logger.CompileWarning(
                    LogMessage.From(nameof(Strings.Warning_BindingIsNotUsed), Strings.Warning_BindingIsNotUsed),
                    ImmutableArray.Create(locationProvider.GetLocation(binding.Source), locationProvider.GetLocation(graph.Source.Source)),
                    LogId.WarningBindingNotUsed);
            }
        }

        return true;
    }

    private static IEnumerable<int> GetIds(MdBinding binding)
    {
        if (!binding.OriginalIds.IsDefaultOrEmpty)
        {
            foreach (var id in binding.OriginalIds)
            {
                yield return id;
            }
        }

        yield return binding.Id;
    }

    private static HashSet<Injection> GetOverriddenInjections(DependencyGraph graph)
    {
        var overridden = new HashSet<Injection>();
        foreach (var node in graph.Graph.Vertices)
        {
            if (node.Factory is not {} factory)
            {
                continue;
            }

            foreach (var @override in factory.Resolvers.SelectMany(resolver => resolver.Overrides).Concat(factory.Initializers.SelectMany(initializer => initializer.Overrides)))
            {
                if (@override.Source.HasExplicitTypeArguments)
                {
                    continue;
                }

                foreach (var injection in @override.Injections)
                {
                    overridden.Add(injection);
                }
            }
        }

        return overridden;
    }

    private static bool IsOverridden(MdBinding binding, HashSet<Injection> overriddenInjections)
    {
        foreach (var contract in binding.Contracts)
        {
            if (contract.ContractType is null)
            {
                continue;
            }

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var tag in contract.Tags.Select(i => i.Value).DefaultIfEmpty(null))
            {
                var injection = new Injection(InjectionKind.Contract, RefKind.None, contract.ContractType, tag, ImmutableArray<Location>.Empty);
                if (overriddenInjections.Contains(injection))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
