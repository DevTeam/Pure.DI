// ReSharper disable UnusedMember.Local
// ReSharper disable RedundantUsingDirective
namespace Pure.DI.Abstractions;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

/// <summary>
///     Pairs a resolved value with the ownership handle that controls the lifetime of the disposable
///     resources created to build it.
/// </summary>
/// <remarks>
///     Inject <see cref="Own{T}" /> instead of <typeparamref name="T" /> when the consumer must own and
///     deterministically dispose the object graph behind a value. Reading <see cref="Value" /> gives access to
///     the resolved instance, while disposing the <see cref="Own{T}" /> disposes every disposable object that
///     was accumulated while creating that value. The struct implements <see cref="IOwn" />, so it also
///     supports asynchronous disposal on frameworks that provide <see cref="IAsyncDisposable" />.
/// </remarks>
/// <param name="value">The owned value.</param>
/// <param name="own">The ownership handle that disposes the resources backing <paramref name="value" />.</param>
/// <typeparam name="T">The type of the owned value.</typeparam>
/// <example>
///     <code>
/// // The service and everything created to build it are released when the handle is disposed.
/// using (Own&lt;IService&gt; owned = composition.OwnedService)
/// {
///     IService service = owned.Value;
///     service.DoWork();
/// }
/// </code>
/// </example>
[DebuggerDisplay("{Value}")]
[DebuggerTypeProxy(typeof(Own<>.DebugView))]
public readonly struct Own<T>(T value, IOwn own) : IOwn
{
    /// <summary>
    ///     The owned value. Its backing resources stay alive until this <see cref="Own{T}" /> is disposed.
    /// </summary>
    public readonly T Value = value;

    private readonly IOwn _own = own;

    /// <inheritdoc />
    public void Dispose() => _own.Dispose();

#if NET || NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    /// <inheritdoc />
    public System.Threading.Tasks.ValueTask DisposeAsync()
    {
        return _own.DisposeAsync();
    }
#endif

#if !NET20 && !NET35 && !NETSTANDARD1_0 && !NETSTANDARD1_1 && !NETSTANDARD1_2 && !NETSTANDARD1_3 && !NETSTANDARD1_4 && !NETSTANDARD1_5 && !NETSTANDARD1_6 && !NETCOREAPP1_0 && !NETCOREAPP1_1
    [ExcludeFromCodeCoverage]
#endif
    private class DebugView(Own<T> own)
    {
        public T Value => own.Value;

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public IOwn Own => own._own;
    }
}
