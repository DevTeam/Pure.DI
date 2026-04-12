namespace Pure.DI.Core.Code;

interface ILocks
{
    void AddLockStatements(bool isStatic, Lines lines, bool isAsync);
}
