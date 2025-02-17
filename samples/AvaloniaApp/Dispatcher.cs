namespace AvaloniaApp;

using Clock.ViewModels;

// ReSharper disable once ClassNeverInstantiated.Global
class Dispatcher : IDispatcher
{
    public void Dispatch(Action action) =>
        Avalonia.Threading.Dispatcher.UIThread.Post(action);
}