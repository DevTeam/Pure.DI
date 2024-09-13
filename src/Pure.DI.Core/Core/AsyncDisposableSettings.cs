// ReSharper disable ClassNeverInstantiated.Global

namespace Pure.DI.Core;

internal class AsyncDisposableSettings(ICache<Compilation, INamedTypeSymbol?> asyncDisposableTypes)
    : IAsyncDisposableSettings
{
    public INamedTypeSymbol? TryGetAsyncDisposableType(Compilation compilation) =>
        asyncDisposableTypes.Get(
            compilation,
            i => i.GetTypeByMetadataName(Names.IAsyncDisposableInterfaceShortName));
}