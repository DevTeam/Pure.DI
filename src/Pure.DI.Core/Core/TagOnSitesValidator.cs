﻿// ReSharper disable ClassNeverInstantiated.Global
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.Core;

sealed class TagOnSitesValidator(
    ILogger logger,
    IRegistry<MdInjectionSite> registry,
    ILocationProvider locationProvider)
    : IValidator<DependencyGraph>
{
    public bool Validate(DependencyGraph data)
    {
        foreach (var tagOn in data.Source.TagOn)
        {
            foreach (var injectionSite in tagOn.InjectionSites.Where(injectionSite => !registry.IsRegistered(data.Source, injectionSite)))
            {
                logger.CompileWarning(
                    string.Format(Strings.Warning_Template_InjectionSiteIsNotUsed, injectionSite.Site),
                    locationProvider.GetLocation(injectionSite.Source),
                    LogId.WarningMetadataDefect);
            }
        }

        return true;
    }
}