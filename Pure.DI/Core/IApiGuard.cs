namespace Pure.DI.Core;

internal interface IApiGuard
{
    bool IsAvailable(Compilation compilation);
}