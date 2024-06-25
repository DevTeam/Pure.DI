// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core;

internal class Registry<T>: IRegistryManager<T>
{
    private readonly HashSet<T> _registered = [];

    public void Register(T value) => _registered.Add(value);
    
    public bool IsRegistered(T value) => _registered.Contains(value);
}