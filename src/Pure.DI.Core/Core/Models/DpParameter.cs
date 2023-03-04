namespace Pure.DI.Core.Models;

internal readonly record struct DpParameter(
    IParameterSymbol ParameterSymbol,
    in Injection Injection)
{
    public override string ToString() => $"{ParameterSymbol.Type} {ParameterSymbol.Name}<--{Injection.ToString()})";
}