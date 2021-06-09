namespace Pure.DI.Core
{
    internal interface IIncludeTypeFilter
    {
        bool IsAccepted(SemanticType factoryType, SemanticType targetType);
    }
}