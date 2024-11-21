// ReSharper disable RedundantUsingDirective
namespace Pure.DI.Abstractions;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Performs accumulation and disposal of disposable objects.
/// </summary>
#if !NET20 && !NET35 && !NETSTANDARD1_0 && !NETSTANDARD1_1 && !NETSTANDARD1_2 && !NETSTANDARD1_3 && !NETSTANDARD1_4 && !NETSTANDARD1_5 && !NETSTANDARD1_6 && !NETCOREAPP1_0 && !NETCOREAPP1_1
[ExcludeFromCodeCoverage]
#endif
public sealed class Own : List<object>, IOwn
{
    private volatile bool _isDisposed;

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        try
        {
            for (var i = Count - 1; i >= 0; i--)
            {
                switch (this[i])
                {
                    case IOwn:
                        break;

                    case IDisposable disposableInstance:
                        try
                        {
                            disposableInstance.Dispose();
                        }
                        catch (Exception)
                        {
                            // ignored
                        }

                        break;

#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
                    case IAsyncDisposable asyncDisposableInstance:
                        try
                        {
                            var valueTask = asyncDisposableInstance.DisposeAsync();
                            if (!valueTask.IsCompleted)
                            {
                                valueTask.AsTask().Wait();
                            }
                        }
                        catch (Exception)
                        {
                            // ignored
                        }
                        break;
#endif
                }
            }
        }
        finally
        {
            Clear();
        }
    }

#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <inheritdoc />
        public async System.Threading.Tasks.ValueTask DisposeAsync()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
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
                                // ignored
                            }
                            break;

                        case IDisposable disposableInstance:
                            try
                            {
                                disposableInstance.Dispose();
                            }
                            catch (Exception)
                            {
                                // ignored
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
}