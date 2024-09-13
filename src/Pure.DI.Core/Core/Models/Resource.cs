namespace Pure.DI.Core.Models;

public readonly record struct Resource(string Name, Stream Content) : IDisposable
{
    public void Dispose() => Content.Dispose();
}