namespace Pure.DI.Core;

internal interface ISourceBuilder
{
    IEnumerable<Source> Build(MetadataContext context);
}