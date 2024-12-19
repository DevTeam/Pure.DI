// ReSharper disable ClassNeverInstantiated.Global
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.Core;

internal class BindingsValidator(ILogger logger, IRegistry<MdBinding> registry)
    : IValidator<DependencyGraph>
{
    public bool Validate(DependencyGraph data)
    {
        foreach (var binding in data.Source.Bindings.Where(i => i.SourceSetup.Kind == CompositionKind.Public && i.Contracts.Any(c => c.Kind == ContractKind.Explicit)))
        {
            if (!registry.IsRegistered(data.Source, binding))
            {
                logger.CompileWarning("The binding was not used.", binding.Source.GetLocation(), LogId.WarningMetadataDefect);
            }
        }

        return true;
    }
}