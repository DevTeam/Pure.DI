namespace Pure.DI.Core.Code;

interface ILocks
{
    bool HasLockField(DependencyGraph dependencyGraph);

    void AddLockStatements(Lines lines, bool isAsync);
}