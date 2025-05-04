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
        foreach (var binding in graph.Source.Bindings.Where(i => i.SourceSetup.Kind == CompositionKind.Public && i.Contracts.Any(c => c.Kind == ContractKind.Explicit)))
        {
            if (!registry.IsRegistered(graph.Source, binding.OriginalId ?? binding.Id))
            {
                logger.CompileWarning(
                    Strings.Warning_BindingIsNotUsed,
                    locationProvider.GetLocation(binding.Source),
                    LogId.WarningMetadataDefect);
            }
        }

        return true;
    }
}