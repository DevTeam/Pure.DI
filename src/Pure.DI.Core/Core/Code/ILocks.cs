namespace Pure.DI.Core.Code;

interface ILocks
{
    void AddLockStatements(bool isStatic, Lines lines, bool isAsync);

    // Gets the expression of the lock of a single instance, see #206.
    string GetInstanceLock(Var var);

    // Gets the name of the field or the variable that holds the lock of a single instance.
    string GetInstanceLockName(string instanceName);

    void AddInstanceLockStatements(string instanceLock, Lines lines);

    void AddInstanceLockDeclaration(Lines lines, string prefix, string name, bool isNullableEnabled, bool isLocal);

    void AddEnsureLockMethod(Lines lines, bool isNullableEnabled);

    // Declares a lock that a factory of an instance that is not shared uses as ctx.Lock.
    void AddLocalLockDeclaration(Lines lines, string name);
}
