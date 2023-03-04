namespace Pure.DI.Core.Models;

internal readonly record struct MdAnyTag(
    SemanticModel SemanticModel,
    SyntaxNode Source)
{
    public override string ToString() => "AnyTag()";
}