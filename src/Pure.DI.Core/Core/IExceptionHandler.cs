namespace Pure.DI.Core;

internal interface IExceptionHandler
{
    void SafeRun<T>(T state, Action<T> action);
}