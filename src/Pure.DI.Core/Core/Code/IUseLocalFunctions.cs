namespace Pure.DI.Core.Code;

interface ILocalFunctions
{
    bool UseFor(CodeContext ctx);

    LocalFunctionScope GetScope(CodeContext ctx);

    string GetKey(CodeContext ctx);

    ImmutableArray<MdOverride> GetOverrideArguments(CodeContext ctx);

    // PerBlock values shared with other instances of the caller's block. Default means that
    // extracting the node would move a conditional injection or an accumulator boundary.
    ImmutableArray<Dependency> GetPerBlockArguments(CodeContext ctx);

    // A transient instance that depends on itself only through lambdas, such as Func<T1, T>, see #192.
    bool IsDeferredCycle(CodeContext ctx);
}
