namespace Pure.DI.Core;

interface INameFormatter
{
    string Format(
        string nameTemplate,
        INamedTypeSymbol? type,
        object? tag);
}