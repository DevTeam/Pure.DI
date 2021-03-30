namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface IObjectBuilder
    {
        ObjectCreationExpressionSyntax TryBuild(INamedTypeSymbol typeSymbol, SemanticModel semanticModel, ITypeResolver typeResolver);
    }
}