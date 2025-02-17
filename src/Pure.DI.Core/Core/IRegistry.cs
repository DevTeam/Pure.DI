namespace Pure.DI.Core;

interface IRegistry<in T>
{
    bool IsRegistered(MdSetup setup, T value);
}