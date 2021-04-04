namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface ITypeResolver
    {
        TypeResolveDescription Resolve(ITypeSymbol typeSymbol, ExpressionSyntax? tag, bool anyTag = false);

        IEnumerable<TypeResolveDescription> Resolve(ITypeSymbol contractTypeSymbol);
    }
}