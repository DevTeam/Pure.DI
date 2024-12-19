namespace Pure.DI.Core;

internal interface IExceptionHandler
{
    TResult? SafeRun<T, TResult>(T state, Func<T, TResult> action);
}