namespace Pure.DI.Core;

internal interface IResourceManager
{
    void Register(IDisposable resource);

    void Clear();
}