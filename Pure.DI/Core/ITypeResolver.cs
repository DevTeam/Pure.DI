namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface ITypeResolver
    {
        TypeResolveDescription Resolve(INamedTypeSymbol typeSymbol, ExpressionSyntax tag, bool anyTag = false);
    }
}