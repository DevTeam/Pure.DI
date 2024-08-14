namespace Pure.DI.Core;

internal interface IAsyncDisposableSettings
{
    INamedTypeSymbol? TryGetAsyncDisposableType(Compilation compilation);
}