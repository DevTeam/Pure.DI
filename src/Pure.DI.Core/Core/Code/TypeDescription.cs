namespace Pure.DI.Core.Code;

public readonly record struct TypeDescription(
    string Name,
    IReadOnlyCollection<string> TypeArgs)
{
    public override string ToString() => Name;
};