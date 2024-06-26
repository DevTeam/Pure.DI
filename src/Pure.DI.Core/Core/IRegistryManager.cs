namespace Pure.DI.Core;

internal interface IRegistryManager<in T>
{
    void Register(MdSetup setup, T value);
}