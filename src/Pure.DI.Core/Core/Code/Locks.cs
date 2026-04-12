// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

sealed class Locks : ILocks
{
    public void AddLockStatements(bool isStatic, Lines lines, bool isAsync)
    {
        if (isAsync)
        {
            return;
        }

        lines.AppendLine($"lock ({(isStatic ? Names.PerResolveLockFieldName : Names.LockFieldName)})");
    }
}
