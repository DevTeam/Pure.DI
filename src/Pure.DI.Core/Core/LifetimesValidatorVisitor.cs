namespace Pure.DI.Core;

internal class LifetimesValidatorVisitor(
    ILogger<LifetimesValidatorVisitor> logger)
    : IPathVisitor<HashSet<object>>
{
    public bool Visit(HashSet<object> errors, in ImmutableArray<DependencyNode> path)
    {
        var actualTargetLifetimeNode = path[0];
        for (var i = 1; i < path.Length; i++)
        {
            var dependencyNode = path[i];
            if (!ValidateLifetimes(actualTargetLifetimeNode.Lifetime, dependencyNode.Lifetime))
            {
                if (errors.Add(new WarningKey(actualTargetLifetimeNode, dependencyNode)))
                {
                    logger.CompileWarning($"Type {actualTargetLifetimeNode.Type} with lifetime {actualTargetLifetimeNode.Lifetime} requires direct or transitive dependency injection of type {dependencyNode.Type} with lifetime {dependencyNode.Lifetime}, which can lead to data leakage and inconsistent behavior.", dependencyNode.Binding.Source.GetLocation(), LogId.WarningLifetimeDefect);
                }
            }

            if (dependencyNode.Lifetime >= actualTargetLifetimeNode.Lifetime)
            {
                actualTargetLifetimeNode = dependencyNode;
            }
        }

        return true;
    }
    
    private static bool ValidateLifetimes(Lifetime actualTargetLifetime, Lifetime dependencyLifetime) => 
        !(actualTargetLifetime == Lifetime.Singleton && dependencyLifetime == Lifetime.Scoped);

    [SuppressMessage("ReSharper", "NotAccessedPositionalProperty.Local")]
    private record WarningKey(DependencyNode TargetNode, DependencyNode SourceNode);
}