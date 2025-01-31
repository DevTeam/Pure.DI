namespace Pure.DI.Core;

internal interface INameFormatter
{
    string Format(
        string nameTemplate,
        INamedTypeSymbol? type,
        object? tag);
}