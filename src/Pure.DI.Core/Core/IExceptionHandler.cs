namespace Pure.DI.Core;

interface IExceptionHandler
{
    TResult? SafeRun<T, TResult>(T state, Func<T, TResult> action);
}