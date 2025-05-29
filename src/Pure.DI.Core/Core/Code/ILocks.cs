namespace Pure.DI.Core.Code;

interface ILocks
{
    bool HasLockField(DependencyGraph dependencyGraph);

    void AddLockStatements(DependencyGraph dependencyGraph, LinesBuilder lines, bool isAsync);

    void AddUnlockStatements(DependencyGraph dependencyGraph, LinesBuilder lines, bool isAsync);
}