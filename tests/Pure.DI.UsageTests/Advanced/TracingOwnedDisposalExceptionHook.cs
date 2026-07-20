// ReSharper disable CheckNamespace

namespace Pure.DI;

sealed partial class Owned
{
    private readonly List<string> _events = [];

    // Called whenever a resource in any Owned<T> graph fails to dispose.
    partial void OnDisposeException<T>(T disposableInstance, Exception exception)
        where T : IDisposable =>
        _events.Add($"{disposableInstance.GetType().Name}: {exception.Message}");

    public IReadOnlyList<string> Events => _events;
}

readonly partial struct Owned<T>
{
    // Exposes diagnostics collected for this Owned<T> graph only.
    public IReadOnlyList<string> Events => ((Owned)owned).Events;
}
