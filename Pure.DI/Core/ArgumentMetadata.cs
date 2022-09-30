namespace Pure.DI.Core;

internal readonly struct ArgumentMetadata
{
    public readonly SemanticType Type;
    public readonly string Name;
    public readonly ExpressionSyntax[] Tags;

    public ArgumentMetadata(SemanticType type, string name, ExpressionSyntax[] tags)
    {
        Type = type;
        Name = name;
        Tags = tags;
    }
}