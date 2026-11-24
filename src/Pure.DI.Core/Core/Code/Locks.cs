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

    public string GetInstanceLock(Var var) =>
        $"{Names.EnsureLockMethodName}(ref {GetInstanceLockName(var.Name)})";

    public string GetInstanceLockName(string instanceName) =>
        instanceName + Names.InstanceLockSuffix;

    public void AddInstanceLockStatements(string instanceLock, Lines lines) =>
        lines.AppendLine($"lock ({instanceLock})");

    public void AddInstanceLockDeclaration(Lines lines, string prefix, string name, bool isNullableEnabled, bool isLocal)
    {
        // A lock is created by the first thread that needs it, so an instance that is never created does not allocate one.
        var nullable = isNullableEnabled ? "?" : "";
        var initializer = isLocal ? " = null" : "";
        lines.AppendLine(new Line(int.MinValue, "#if NET9_0_OR_GREATER"));
        lines.AppendLine($"{prefix}{Names.LockTypeName}{nullable} {name}{initializer};");
        lines.AppendLine(new Line(int.MinValue, "#else"));
        lines.AppendLine($"{prefix}{Names.ObjectTypeName}{nullable} {name}{initializer};");
        lines.AppendLine(new Line(int.MinValue, "#endif"));
    }

    public void AddEnsureLockMethod(Lines lines, bool isNullableEnabled)
    {
        var nullable = isNullableEnabled ? "?" : "";
        lines.AppendLine(new Line(int.MinValue, "#if NET9_0_OR_GREATER"));
        AddEnsureLockMethod(lines, Names.LockTypeName, nullable);
        lines.AppendLine(new Line(int.MinValue, "#else"));
        AddEnsureLockMethod(lines, Names.ObjectTypeName, nullable);
        lines.AppendLine(new Line(int.MinValue, "#endif"));
    }

    public void AddLocalLockDeclaration(Lines lines, string name)
    {
        lines.AppendLine(new Line(int.MinValue, "#if NET9_0_OR_GREATER"));
        lines.AppendLine($"var {name} = new {Names.LockTypeName}();");
        lines.AppendLine(new Line(int.MinValue, "#else"));
        lines.AppendLine($"var {name} = new {Names.ObjectTypeName}();");
        lines.AppendLine(new Line(int.MinValue, "#endif"));
    }

    private static void AddEnsureLockMethod(Lines lines, string lockType, string nullable)
    {
        lines.AppendLine($"[{Names.MethodImplAttributeName}(({Names.MethodImplOptionsName})256)]");
        lines.AppendLine($"private static {lockType} {Names.EnsureLockMethodName}(ref {lockType}{nullable} lockObject) =>");
        lines.IncIndent();
        lines.AppendLine($"lockObject ?? {Names.SystemNamespace}Threading.Interlocked.CompareExchange(ref lockObject, new {lockType}(), null) ?? lockObject{(nullable.Length > 0 ? "!" : "")};");
        lines.DecIndent();
    }
}
