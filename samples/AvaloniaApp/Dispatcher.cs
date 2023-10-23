namespace AvaloniaApp;

using Clock.ViewModels;

// ReSharper disable once ClassNeverInstantiated.Global
internal class Dispatcher : IDispatcher
{
    private readonly Avalonia.Threading.IDispatcher _dispatcher;

    public Dispatcher(Avalonia.Threading.IDispatcher dispatcher) => 
        _dispatcher = dispatcher;

    public void Dispatch(Action action) => _dispatcher.Post(action);
}