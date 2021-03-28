namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface IObjectBuilder
    {
        ObjectCreationExpressionSyntax TryBuild(ITypeSymbol typeSymbol, SemanticModel semanticModel, ITypeResolver typeResolver);
    }
}