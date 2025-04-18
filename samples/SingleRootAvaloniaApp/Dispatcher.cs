namespace AvaloniaApp;

using Clock.Models;

// ReSharper disable once ClassNeverInstantiated.Global
class Dispatcher : IDispatcher
{
    public void Dispatch(Action action) =>
        Avalonia.Threading.Dispatcher.UIThread.Post(action);
}