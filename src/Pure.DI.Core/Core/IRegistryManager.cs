namespace Pure.DI.Core;

interface IRegistryManager<in T>
{
    void Register(MdSetup setup, T value);
}