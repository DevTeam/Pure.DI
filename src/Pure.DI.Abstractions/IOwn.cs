namespace Pure.DI.Abstractions;

using System;

/// <summary>
///     Represents ownership of one or more disposable resources and releases them on disposal.
/// </summary>
/// <remarks>
///     An <see cref="IOwn" /> handle groups the disposable objects created while resolving a composition root
///     and disposes them deterministically when the handle itself is disposed. Disposing the handle releases
///     every owned resource in reverse order of creation. On target frameworks that support it the handle also
///     implements <see cref="IAsyncDisposable" /> for asynchronous release. See also <see cref="Own{T}" />,
///     which pairs a resolved value with its ownership handle.
/// </remarks>
/// <example>
///     <code>
/// using (IOwn own = /* resolved ownership handle */)
/// {
///     // Use the owned resources here.
/// } // All owned resources are disposed here.
/// </code>
/// </example>
public interface IOwn :
    IDisposable
#if NET || NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        , IAsyncDisposable
#endif
{
}
