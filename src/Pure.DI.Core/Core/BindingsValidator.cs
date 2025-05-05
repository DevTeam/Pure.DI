// ReSharper disable ClassNeverInstantiated.Global
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.Core;

sealed class BindingsValidator(
    ILogger logger,
    IRegistry<int> registry,
    ILocationProvider locationProvider)
    : IValidator<DependencyGraph>
{
    public bool Validate(DependencyGraph graph)
    {
        foreach (var binding in graph.Source.Bindings.Where(i => i.SourceSetup.Kind == CompositionKind.Public && i.Contracts.All(c => c.Kind == ContractKind.Explicit)))
        {
            if (!GetIds(binding).Any(id => registry.IsRegistered(graph.Source, id)))
            {
                logger.CompileWarning(
                    Strings.Warning_BindingIsNotUsed,
                    locationProvider.GetLocation(binding.Source),
                    LogId.WarningMetadataDefect);
            }
        }

        return true;
    }

    private IEnumerable<int> GetIds(MdBinding binding)
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
}