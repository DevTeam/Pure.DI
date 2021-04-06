namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface IResolverBuilder
    {
        CompilationUnitSyntax Build();
    }
}