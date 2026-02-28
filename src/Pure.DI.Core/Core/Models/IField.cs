namespace Pure.DI.Core.Models;

interface IField
{
    string Name { get; }

    ISymbol? Type { get; }

    bool IsRequired { get; }

    Accessibility DeclaredAccessibility { get; }

    bool HasConstantValue { get; }

    object? ConstantValue { get; }

    ImmutableArray<Location> Locations { get; }

    INamedTypeSymbol ContainingType { get; }
}