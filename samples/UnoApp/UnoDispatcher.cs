namespace UnoApp;

sealed class UnoDispatcher : IDispatcher
{
    private readonly DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();

    public void Dispatch(Action action)
    {
        if (_dispatcher.HasThreadAccess)
        {
            action();
            return;
        }

        _dispatcher.TryEnqueue(() => action());
    }
}
