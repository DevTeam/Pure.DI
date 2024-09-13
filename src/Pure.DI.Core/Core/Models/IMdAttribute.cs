// ReSharper disable UnusedMember.Global

namespace Pure.DI.Core.Models;

internal interface IMdAttribute
{
    SemanticModel SemanticModel { get; }

    SyntaxNode Source { get; }

    INamedTypeSymbol AttributeType { get; }

    int ArgumentPosition { get; }
}