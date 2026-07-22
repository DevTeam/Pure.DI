// ReSharper disable RedundantUsingDirective
namespace Pure.DI.Abstractions;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

/// <summary>
///     Performs accumulation and deterministic disposal of owned objects.
/// </summary>
#if !NET20 && !NET35 && !NETSTANDARD1_0 && !NETSTANDARD1_1 && !NETSTANDARD1_2 && !NETSTANDARD1_3 && !NETSTANDARD1_4 && !NETSTANDARD1_5 && !NETSTANDARD1_6 && !NETCOREAPP1_0 && !NETCOREAPP1_1
[ExcludeFromCodeCoverage]
#endif
public sealed class Own : List<object>, IOwn
{
    /// <summary>
    ///     A shared no-op owner for a graph known at generation time to contain no resources.
    /// </summary>
    public static readonly Own Empty = new Own(isDisposed: true);

    private int _isDisposed;
#if NET9_0_OR_GREATER
    private readonly System.Threading.Lock? _synchronization;
#else
    private readonly object? _synchronization;
#endif

    /// <summary>
    ///     Initializes an empty owner.
    /// </summary>
    public Own()
    {
    }

    /// <summary>
    ///     Initializes an empty owner with the specified initial capacity.
    /// </summary>
    /// <param name="capacity">The initial number of resources the owner can store without resizing.</param>
    public Own(int capacity)
        : base(capacity)
    {
    }

    /// <summary>
    ///     Initializes an empty owner with the specified initial capacity and synchronization object.
    /// </summary>
    /// <param name="capacity">The initial number of resources the owner can store without resizing.</param>
    /// <param name="synchronization">Coordinates resource registration with disposal.</param>
#if NET9_0_OR_GREATER
    public Own(int capacity, System.Threading.Lock synchronization)
#else
    public Own(int capacity, object synchronization)
#endif
        : base(capacity)
    {
        _synchronization = synchronization;
    }

    private Own(bool isDisposed)
    {
        _isDisposed = isDisposed ? 1 : 0;
    }

    /// <summary>
    ///     Registers an owned resource or rejects and disposes it when this owner has already been disposed.
    /// </summary>
    /// <param name="item">The resource to own.</param>
    public new void Add(object item)
    {
        if (_isDisposed == 0)
        {
            base.Add(item);
            return;
        }

        Reject(item);
    }

    /// <summary>
    ///     Verifies that an ownership handle is created while this owner is active without retaining
    ///     the independent owner in this collection.
    /// </summary>
    /// <param name="item">The independent ownership handle.</param>
    public void Add(IOwn item)
    {
        if (_isDisposed == 0) return;
        Reject(item);
    }

    /// <summary>
    ///     Verifies that an <see cref="Own{T}"/> handle is created while this owner is active without
    ///     retaining or boxing the handle.
    /// </summary>
    /// <param name="item">The independent ownership handle.</param>
    /// <typeparam name="T">The type of the owned value.</typeparam>
    public void Add<T>(Own<T> item)
    {
        if (_isDisposed == 0) return;
        Reject(item);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!TryBeginDispose()) return;
        try
        {
            for (var i = Count - 1; i >= 0; i--)
            {
                var item = this[i];
                if (!(item is IOwn))
                {
                    DisposeSynchronously(item);
                }
            }
        }
        finally
        {
            Clear();
        }
    }

#if NET || NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    /// <inheritdoc />
    public async System.Threading.Tasks.ValueTask DisposeAsync()
    {
        if (!TryBeginDispose()) return;
        try
        {
            for (var i = Count - 1; i >= 0; i--)
            {
                switch (this[i])
                {
                    case IOwn:
                        break;

                    case IAsyncDisposable asyncDisposableInstance:
                        try
                        {
                            await asyncDisposableInstance.DisposeAsync();
                        }
                        catch (Exception)
                        {
                            // A failing resource must not interrupt disposal of the remaining graph.
                        }

                        break;

                    case IDisposable disposableInstance:
                        try
                        {
                            disposableInstance.Dispose();
                        }
                        catch (Exception)
                        {
                            // A failing resource must not interrupt disposal of the remaining graph.
                        }

                        break;
                }
            }
        }
        finally
        {
            Clear();
        }
    }
#endif

    private bool TryBeginDispose()
    {
        var synchronization = _synchronization;
        if (synchronization is null)
        {
            return TryBeginDisposeCore();
        }

        lock (synchronization)
        {
            return TryBeginDisposeCore();
        }
    }

    private bool TryBeginDisposeCore()
    {
        if (_isDisposed != 0)
        {
            return false;
        }

        _isDisposed = 1;
        return true;
    }

    private void DisposeSynchronously(object item)
    {
        if (ReferenceEquals(item, this)) return;
        switch (item)
        {
            case IDisposable disposableInstance:
                try
                {
                    disposableInstance.Dispose();
                }
                catch (Exception)
                {
                    // A failing resource must not interrupt disposal of the remaining graph.
                }

                break;

#if NET || NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            case IAsyncDisposable asyncDisposableInstance:
                try
                {
                    asyncDisposableInstance.DisposeAsync().GetAwaiter().GetResult();
                }
                catch (Exception)
                {
                    // A failing resource must not interrupt disposal of the remaining graph.
                }

                break;
#endif
        }
    }

    private void Reject(object item)
    {
        DisposeSynchronously(item);
        throw new ObjectDisposedException(nameof(Own));
    }
}
