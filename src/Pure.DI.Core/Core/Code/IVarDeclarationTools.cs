namespace Pure.DI.Core.Code;

interface IVarDeclarationTools
{
    IEnumerable<VarDeclaration> Sort(IEnumerable<VarDeclaration> declarations);
}