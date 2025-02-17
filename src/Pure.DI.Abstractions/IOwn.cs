namespace Pure.DI.Abstractions;

using System;

/// <summary>
///     This abstraction allows a disposable object to be disposed of.
/// </summary>
public interface IOwn :
    IDisposable
#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        , IAsyncDisposable
#endif
{
}