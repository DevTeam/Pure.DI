namespace Pure.DI.Core
{
    using System.Collections.Generic;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface ITypeResolver
    {
        TypeDescription Resolve(ITypeSymbol typeSymbol, ExpressionSyntax? tag, bool anyTag = false, bool suppressWarnings = false);

        IEnumerable<TypeDescription> Resolve(ITypeSymbol contractTypeSymbol);
    }
}