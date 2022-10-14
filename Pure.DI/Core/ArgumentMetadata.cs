namespace Pure.DI.Core;

internal readonly struct ArgumentMetadata
{
    public readonly SemanticType Type;
    public readonly string Name;

    public ArgumentMetadata(SemanticType type, string name)
    {
        Type = type;
        Name = name;
    }
}