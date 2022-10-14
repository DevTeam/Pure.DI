// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal class MetadataFactory : IMetadataFactory
{
    public ResolverMetadata Create(ResolverMetadata metadata, IReadOnlyCollection<ResolverMetadata> allMetadata)
    {
        var newMetadata = new ResolverMetadata(metadata.SetupNode, metadata.ComposerTypeName, metadata.Owner);
        var dependencies = GetDependencies(metadata, new HashSet<ResolverMetadata>(), allMetadata);
        foreach (var dependency in dependencies)
        {
            newMetadata.Merge(dependency);
        }

        newMetadata.Merge(metadata);
        return newMetadata;
    }
    
    private static IEnumerable<ResolverMetadata> GetDependencies(ResolverMetadata metadata, ISet<ResolverMetadata> processed, IReadOnlyCollection<ResolverMetadata> allMetadata)
    {
        var dependencies =
            from dependencyName in metadata.DependsOn
            from dependency in allMetadata
            where dependencyName.Equals(dependency.ComposerTypeName, StringComparison.InvariantCultureIgnoreCase)
            select dependency;

        foreach (var dependency in dependencies)
        {
            if (!processed.Add(dependency))
            {
                continue;
            }

            yield return dependency;

            foreach (var nested in GetDependencies(dependency, processed, allMetadata))
            {
                yield return nested;
            }
        }
    }
}