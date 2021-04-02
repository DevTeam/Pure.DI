namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface INameService
    {
        string FindName(string prefix, INamedTypeSymbol contractType, ExpressionSyntax? tag);
    }
}