namespace Pure.DI.Core.Code;

internal interface ILocks
{
    string GetLockType(MdSetup setup);

    void AddLockStatements(MdSetup setup, LinesBuilder lines, bool isAsync);
    
    void AddUnlockStatements(MdSetup setup, LinesBuilder lines, bool isAsync);
}