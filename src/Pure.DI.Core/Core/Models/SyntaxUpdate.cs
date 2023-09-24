namespace Pure.DI.Core.Models;

internal readonly record struct SyntaxUpdate(
    SyntaxNode Node,
    SemanticModel SemanticModel);