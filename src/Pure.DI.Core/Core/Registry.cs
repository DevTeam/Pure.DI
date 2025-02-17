// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable NotAccessedPositionalProperty.Local

namespace Pure.DI.Core;

sealed class Registry<T> : IRegistryManager<T>, IRegistry<T>
{
    private readonly HashSet<Key> _registered = [];

    public bool IsRegistered(MdSetup setup, T value)
    {
        lock (_registered)
        {
            return _registered.Contains(new Key(setup.Name, value));
        }
    }

    public void Register(MdSetup setup, T value)
    {
        lock (_registered)
        {
            _registered.Add(new Key(setup.Name, value));
        }
    }

    private readonly record struct Key(
        CompositionName CompositionName,
        T Value);
}