namespace AvaloniaApp;

class AvaloniaDispatcher : IDispatcher
{
    public void Dispatch(Action action) =>
        Avalonia.Threading.Dispatcher.UIThread.Invoke(action);
}