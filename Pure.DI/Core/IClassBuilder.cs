namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface IClassBuilder
    {
        CompilationUnitSyntax Build(SemanticModel semanticModel);
    }
}