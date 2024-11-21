namespace Pure.DI.Core.Code;

internal class Locks(ITypes types) : ILocks
{
    public string GetLockType(Compilation compilation) =>
        types.TryGet(SpecialType.Lock, compilation) is not null
            ? Names.LockTypeName
            : Names.ObjectTypeName;

    public void AddLockStatements(Compilation compilation, LinesBuilder lines, bool isAsync)
    {
        if (!isAsync)
        {
            lines.AppendLine(
                types.TryGet(SpecialType.Lock, compilation) is not null
                    ? $"using ({Names.LockFieldName}.EnterScope())"
                    : $"lock ({Names.LockFieldName})");
        }
        else
        {
            lines.AppendLine($"{Names.LockFieldName}.Enter();");
            lines.AppendLine($"try");
        }
    }

    public void AddUnlockStatements(Compilation compilation, LinesBuilder lines, bool isAsync)
    {
        if (!isAsync)
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
}