namespace Pure.DI.Core.Code;

interface ILocks
{
    string GetLockType(MdSetup setup);

    void AddLockStatements(MdSetup setup, LinesBuilder lines, bool isAsync);

    void AddUnlockStatements(MdSetup setup, LinesBuilder lines, bool isAsync);
}