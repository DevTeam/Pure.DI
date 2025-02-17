namespace Pure.DI.Core.Models;

record DpField(
    IFieldSymbol Field,
    int? Ordinal,
    Injection Injection)
{
    public override string ToString() => $"{Field}<--{Injection.ToString()}";
}