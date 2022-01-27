namespace Pure.DI.Core;

internal interface IStaticResolverNameProvider
{
    string GetName(SemanticType dependency);
}