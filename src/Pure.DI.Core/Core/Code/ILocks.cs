namespace Pure.DI.Core.Code;

interface ILocks
{
    bool HasLockField(DependencyGraph dependencyGraph);

    void AddLockStatements(bool isStatic, Lines lines, bool isAsync);
}