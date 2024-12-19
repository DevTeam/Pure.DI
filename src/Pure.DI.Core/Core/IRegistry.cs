namespace Pure.DI.Core;

internal interface IRegistry<T>
{
    bool IsRegistered(MdSetup setup, T value);

    IEnumerable<T> GetRegistrations();
}