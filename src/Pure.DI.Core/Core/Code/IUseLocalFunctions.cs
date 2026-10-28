namespace Pure.DI.Core.Code;

interface ILocalFunctions
{
    bool UseFor(CodeContext ctx);

    LocalFunctionScope GetScope(CodeContext ctx);
}