namespace Pure.DI.Core.Code;

interface ILocks
{
    bool HasLockField(DependencyGraph dependencyGraph);

    void AddLockStatements(LinesBuilder lines, bool isAsync);
}