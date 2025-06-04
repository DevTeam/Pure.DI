namespace Pure.DI.Core;

interface INameFormatter
{
    string Format(
        string nameTemplate,
        ITypeSymbol? type,
        object? tag);
}