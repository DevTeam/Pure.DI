namespace Pure.DI.Core;

internal interface IClassBuilder
{
    CompilationUnitSyntax Build(SemanticModel semanticModel);
}