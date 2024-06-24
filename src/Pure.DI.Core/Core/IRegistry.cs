namespace Pure.DI.Core;

internal interface IRegistry<in T>
{
    bool IsRegistered(T value);
}