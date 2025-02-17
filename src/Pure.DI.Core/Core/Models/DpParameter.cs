namespace Pure.DI.Core.Models;

record DpParameter(
    IParameterSymbol ParameterSymbol,
    in Injection Injection)
{
    public override string ToString() => $"{ParameterSymbol.Type} {ParameterSymbol.Name}<--{Injection.ToString()})";
}