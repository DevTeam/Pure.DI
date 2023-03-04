namespace Pure.DI.Core.Models;

internal readonly record struct DpField(
    IFieldSymbol Field,
    int? Order,
    Injection Injection)
{
    public override string ToString() => $"{Field}<--{Injection.ToString()}";
}