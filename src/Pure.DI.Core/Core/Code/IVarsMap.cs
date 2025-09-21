namespace Pure.DI.Core.Code;

interface IVarsMap
{
    IEnumerable<Var> Vars { get; }

    IEnumerable<VarDeclaration> Declarations { get; }

    bool IsThreadSafe { get; }

    VarInjection GetInjection(DependencyGraph graph, Root root, in Injection injection, IDependencyNode node);

    IDisposable Root(Lines lines);

    IDisposable LocalFunction(Var var, Lines lines);

    IDisposable Lazy(Var var, Lines lines);

    IDisposable Block(Var var, Lines lines);
}