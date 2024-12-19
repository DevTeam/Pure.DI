// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable NotAccessedPositionalProperty.Local

namespace Pure.DI.Core;

internal class Registry<T> : IRegistryManager<T>, IRegistry<T>
{
    private readonly HashSet<Key> _registered = [];

    public void Register(MdSetup setup, T value)
    {
        lock (_registered)
        {
            _registered.Add(new Key(setup.Name, value));
        }
    }

    public bool IsRegistered(MdSetup setup, T value)
    {
        lock (_registered)
        {
            return _registered.Contains(new Key(setup.Name, value));
        }
    }

    public IEnumerable<T> GetRegistrations()
    {
        List<T> result;
        lock (_registered)
        {
            result = _registered.Select(i => i.Value).ToList();
        }

        return result;
    }

    private readonly record struct Key(CompositionName CompositionName, T Value);
}