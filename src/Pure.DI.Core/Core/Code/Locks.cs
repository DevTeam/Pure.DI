// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

sealed class Locks(IOverridesRegistry overridesRegistry) : ILocks
{
    public bool HasLockField(DependencyGraph dependencyGraph) =>
        dependencyGraph.Roots.Any(root => overridesRegistry.GetOverrides(root).Any());

    public void AddLockStatements(bool isStatic, Lines lines, bool isAsync)
    {
        if (isAsync)
        {
            return;
        }

        lines.AppendLine($"lock ({(isStatic ? Names.PerResolveLockFieldName : Names.LockFieldName)})");
    }
}