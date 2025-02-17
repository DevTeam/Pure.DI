// ReSharper disable UnusedMember.Local
// ReSharper disable RedundantUsingDirective
namespace Pure.DI.Abstractions;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

/// <summary>
///     Contains a value and gives the ability to dispose of that value.
/// </summary>
/// <param name="value">Own value.</param>
/// <param name="own">The abstraction allows a disposable object to be disposed of.</param>
/// <typeparam name="T">Type of value owned.</typeparam>
[DebuggerDisplay("{Value}")]
[DebuggerTypeProxy(typeof(Own<>.DebugView))]
public readonly struct Own<T>(T value, IOwn own) : IOwn
{
    /// <summary>
    ///     Own value.
    /// </summary>
    public readonly T Value = value;

    private readonly IOwn _own = own;

    /// <inheritdoc />
    public void Dispose() => _own.Dispose();

#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
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