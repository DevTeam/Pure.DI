namespace Pure.DI.Core.Models;

internal readonly record struct DpProperty(
    IPropertySymbol Property,
    int? Order,
    in Injection Injection)
{
    public override string ToString() => $"set_{Property}(<--{Injection.ToString()})";
}