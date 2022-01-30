namespace Pure.DI.Core;

internal readonly struct AttributeMetadata
{
    public readonly AttributeKind Kind;
    public readonly INamedTypeSymbol Type;
    public readonly int ArgumentPosition;

    public AttributeMetadata(AttributeKind kind, INamedTypeSymbol type, int argumentPosition)
    {
        Kind = kind;
        Type = type;
        ArgumentPosition = argumentPosition;
    }
}