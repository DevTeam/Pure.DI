namespace Pure.DI.Core;

internal interface IRegistryManager<in T>: IRegistry<T>
{
    void Register(T value);
}