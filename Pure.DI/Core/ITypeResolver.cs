namespace Pure.DI.Core;

internal interface ITypeResolver
{
    Dependency Resolve(SemanticType dependency, ExpressionSyntax? tag, bool anyTag = false);

    IEnumerable<Dependency> Resolve(SemanticType dependency);
}