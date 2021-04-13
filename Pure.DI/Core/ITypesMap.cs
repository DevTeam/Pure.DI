namespace Pure.DI.Core
{
    internal interface ITypesMap
    {
        bool Setup(SemanticType dependency, SemanticType implementation);

        SemanticType ConstructType(SemanticType type);
    }
}