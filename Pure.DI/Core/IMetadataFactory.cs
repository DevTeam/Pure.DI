namespace Pure.DI.Core;

internal interface IMetadataFactory
{
    ResolverMetadata Create(ResolverMetadata metadata, IReadOnlyCollection<ResolverMetadata> allMetadata);
}