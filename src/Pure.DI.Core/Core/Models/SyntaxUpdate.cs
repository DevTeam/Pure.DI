namespace Pure.DI.Core.Models;

readonly record struct SyntaxUpdate(
    SyntaxNode Node,
    SemanticModel SemanticModel);