// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable NotAccessedPositionalProperty.Local

namespace Pure.DI.Core;

internal class Registry<T> : IRegistryManager<T>, IRegistry<T>
{
    private readonly HashSet<Key> _registered = [];

    public void Register(MdSetup setup, T value) => _registered.Add(new Key(setup.Name, value));

    public bool IsRegistered(MdSetup setup, T value) => _registered.Contains(new Key(setup.Name, value));

    private readonly record struct Key(CompositionName CompositionName, T Value);
}