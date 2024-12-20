﻿namespace Pure.DI.Core.Code;

internal interface ILocks
{
    string GetLockType(Compilation compilation);

    void AddLockStatements(Compilation compilation, LinesBuilder lines, bool isAsync);
    
    void AddUnlockStatements(LinesBuilder lines, bool isAsync);
}