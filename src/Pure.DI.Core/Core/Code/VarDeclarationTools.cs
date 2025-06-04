namespace Pure.DI.Core.Code;

class VarDeclarationTools : IVarDeclarationTools
{
    public IEnumerable<VarDeclaration> Sort(IEnumerable<VarDeclaration> declarations) =>
        declarations
            .GroupBy(i => i.Node.Binding.Id)
            .Select(i => i.First())
            .OrderBy(i => !(i.Node.Arg?.Source.IsBuildUpInstance ?? false))
            .ThenBy(i => i.Node.Binding.Id);
}