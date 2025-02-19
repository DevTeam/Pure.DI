// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

sealed class Locks(ITypes types) : ILocks
{
    public string GetLockType(MdSetup setup) =>
        IsSystemThreadingLockEnabled(setup)
            ? Names.LockTypeName
            : Names.ObjectTypeName;

    public void AddLockStatements(MdSetup setup, LinesBuilder lines, bool isAsync)
    {
        if (!isAsync)
        {
            lines.AppendLine(
                IsSystemThreadingLockEnabled(setup)
                    ? $"using ({Names.LockFieldName}.EnterScope())"
                    : $"lock ({Names.LockFieldName})");
        }
        else
        {
            // ReSharper disable once InvertIf
            if (IsSystemThreadingLockEnabled(setup))
            {
                lines.AppendLine($"{Names.LockFieldName}.Enter();");
                lines.AppendLine("try");
            }
        }
    }

    public void AddUnlockStatements(MdSetup setup, LinesBuilder lines, bool isAsync)
    {
        if (!isAsync || !IsSystemThreadingLockEnabled(setup))
        {
            return;
        }

        lines.AppendLine("finally");
        lines.AppendLine("{");
        using (lines.Indent())
        {
            lines.AppendLine($"{Names.LockFieldName}.Exit();");
        }

        lines.AppendLine("}");
    }

    private bool IsSystemThreadingLockEnabled(MdSetup setup) =>
        setup.Hints.IsSystemThreadingLockEnabled
        && types.TryGet(SpecialType.Lock, setup.SemanticModel.Compilation) is not null;
}