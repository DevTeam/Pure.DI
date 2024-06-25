#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.Core;

internal class TagOnSitesValidator(ILogger<TagOnSitesValidator> logger, IRegistryManager<MdInjectionSite> registry)
    : IValidator<DependencyGraph>
{
    public bool Validate(DependencyGraph data)
    {
        foreach (var tagOn in data.Source.TagOn)
        {
            foreach (var injectionSite in tagOn.InjectionSites.Where(injectionSite => !registry.IsRegistered(injectionSite)))
            {
                logger.CompileWarning($"\"{injectionSite.Site}\" of the tag on the injection site was not used.", injectionSite.Source.GetLocation(), LogId.WarningMetadataDefect);
            }
        }

        return true;
    }
}