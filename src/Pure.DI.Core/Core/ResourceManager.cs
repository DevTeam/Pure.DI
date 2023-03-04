namespace Pure.DI.Core;

internal class ResourceManager : IResourceManager
{
    private readonly List<IDisposable> _resources = new();
    
    public void Register(IDisposable resource) => _resources.Add(resource);

    public void Clear()
    {
        _resources.Reverse();
        foreach (var resource in _resources)
        {
            try
            {
                resource.Dispose();
            }
            catch
            {
                // ignored
            }
        }
        
        _resources.Clear();
    }
}