namespace Pure.DI.Core;

internal interface IAttributes
{
    T GetAttribute<TMdAttribute, T>(
        in ImmutableArray<TMdAttribute> metadata,
        ISymbol member,
        T defaultValue)
        where TMdAttribute : IMdAttribute;

    T GetAttribute<TMdAttribute, T>(
        SemanticModel semanticModel,
        in ImmutableArray<TMdAttribute> metadata,
        in ImmutableArray<AttributeSyntax> attributes,
        T defaultValue)
        where TMdAttribute : IMdAttribute;
}