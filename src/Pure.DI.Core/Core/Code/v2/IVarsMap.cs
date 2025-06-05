namespace Pure.DI.Core.Code.v2;

interface IVarsMap
{
    IEnumerable<MdBinding> GetBindings();

    IEnumerable<VarDeclaration> GetSingletons();

    IEnumerable<VarDeclaration> GetPerResolves();

    IEnumerable<VarDeclaration> GetArgs();

    Var GetVar(in Injection injection, DependencyNode node, DeclarationPath path);

    CodeChanges Reset(CodeChanges changes, bool resetSingletons, DeclarationPath path);

    void Remove(IEnumerable<MdBinding> bindings);

    IEnumerable<VarDeclaration> Sort(IEnumerable<VarDeclaration> declarations);
}