namespace Pure.DI.Core;

internal class TagOnSitesValidator(ILogger<TagOnSitesValidator> logger)
    : IValidator<DependencyGraph>
{
    public bool Validate(DependencyGraph data)
    {
        foreach (var tagOn in data.Source.TagOn)
        {
            foreach (var notUsed in tagOn.NotUsed)
            {
                logger.CompileWarning($"\"{notUsed.Site}\" of the tag on the injection site was not used.", notUsed.Source.GetLocation(), LogId.WarningMetadataDefect);
            }
        }

        return true;
    }
}