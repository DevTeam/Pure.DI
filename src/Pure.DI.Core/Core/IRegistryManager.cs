namespace Pure.DI.Core;

internal interface IRegistryManager<in T>
{
    void Register(T value);
}