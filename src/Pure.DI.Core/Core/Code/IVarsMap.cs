namespace Pure.DI.Core.Code;

interface IVarsMap
{
    IEnumerable<Var> Vars { get; }

    IEnumerable<VarDeclaration> Declarations { get; }

    bool IsThreadSafe { get; }

    VarInjection GetVar(in Injection injection, IDependencyNode node);

    IDisposable Root(LinesBuilder lines);

    IDisposable LocalFunction(Var var, LinesBuilder lines);

    IDisposable Lazy(Var var, LinesBuilder lines);

    IDisposable Block(Var var, LinesBuilder lines);
}