namespace Pure.DI.Core;

internal interface IRegistry<in T>
{
    bool IsRegistered(MdSetup setup, T value);
}