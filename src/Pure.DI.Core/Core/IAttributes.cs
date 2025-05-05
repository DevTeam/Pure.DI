namespace Pure.DI.Core;

interface IAttributes
{
    T GetAttribute<TMdAttribute, T>(
        SemanticModel semanticModel,
        in ImmutableArray<TMdAttribute> metadata,
        ISymbol member,
        bool isTag,
        T defaultValue)
        where TMdAttribute : IMdAttribute;

    T GetAttribute<TMdAttribute, T>(
        SemanticModel semanticModel,
        in ImmutableArray<TMdAttribute> metadata,
        in ImmutableArray<AttributeSyntax> attributes,
        T defaultValue)
        where TMdAttribute : IMdAttribute;
}