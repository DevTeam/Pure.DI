namespace Pure.DI.Core;

internal interface IAsyncDisposableSettings
{
    INamedTypeSymbol? TryGetAsyncDisposableType(Compilation compilation);

    bool IsEnabled(Compilation compilation);
}