namespace Pure.DI.Core;

interface IAttributes
{
    T GetAttribute<TMdAttribute, T>(
        SemanticModel semanticModel,
        in ImmutableArray<TMdAttribute> metadata,
        ISymbol member,
        AttributeKind kind,
        T defaultValue)
        where TMdAttribute : IMdAttribute;

    T GetAttribute<TMdAttribute, T>(
        SemanticModel semanticModel,
        in ImmutableArray<TMdAttribute> metadata,
        in ImmutableArray<AttributeSyntax> attributes,
        AttributeKind kind,
        T defaultValue)
        where TMdAttribute : IMdAttribute;
}