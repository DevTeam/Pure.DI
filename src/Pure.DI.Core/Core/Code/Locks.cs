// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

sealed class Locks(IOverridesRegistry overridesRegistry) : ILocks
{
    public bool HasLockField(DependencyGraph dependencyGraph) =>
        dependencyGraph.Roots.Any(root => overridesRegistry.GetOverrides(root).Any())
        || dependencyGraph.Graph.Edges.Any(dependency => Tag.SyncRoot.Equals(dependency.Injection.Tag));

    public void AddLockStatements(Lines lines, bool isAsync)
    {
        if (!isAsync)
        {
            lines.AppendLine($"lock ({Names.LockFieldName})");
        }
    }
}