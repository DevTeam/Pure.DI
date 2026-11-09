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
}
