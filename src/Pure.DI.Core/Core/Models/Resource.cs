namespace Pure.DI.Core.Models;

internal readonly record struct Resource(string Name, Stream Content) : IDisposable
{
    public void Dispose() => Content.Dispose();
}