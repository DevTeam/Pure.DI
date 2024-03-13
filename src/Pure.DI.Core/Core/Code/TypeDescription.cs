namespace Pure.DI.Core.Code;

public readonly record struct TypeDescription(
    string Name,
    IReadOnlyCollection<TypeDescription> TypeArgs)
{
    public override string ToString() => Name;
};