// ReSharper disable UnusedMember.Global

namespace Pure.DI.Core.Models;

interface IMdAttribute
{
    SemanticModel SemanticModel { get; }

    InvocationExpressionSyntax Source { get; }

    INamedTypeSymbol AttributeType { get; }

    int ArgumentPosition { get; }
}