namespace Pure.DI.Core.Models;

internal readonly record struct DpProperty(
    IPropertySymbol Property,
    int? Ordinal,
    in Injection Injection)
{
    public override string ToString() => $"set_{Property}(<--{Injection.ToString()})";
}