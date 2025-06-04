namespace Pure.DI.Core.Code;

record VarInjection(Var Var, Injection Injection)
{
    public ITypeSymbol ContractType => Injection.Type;

    public override string ToString() => $"{ContractType} {Var.Name}({Injection.Tag})";
}