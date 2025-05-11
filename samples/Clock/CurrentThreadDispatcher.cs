namespace Clock;

public sealed class CurrentThreadDispatcher: IDispatcher
{
    public void Dispatch(Action action) => action();
}