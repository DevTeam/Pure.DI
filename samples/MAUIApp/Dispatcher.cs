namespace MAUIApp;

using IDispatcher = Microsoft.Maui.Dispatching.IDispatcher;

// ReSharper disable once ClassNeverInstantiated.Global
internal class Dispatcher : Clock.ViewModels.IDispatcher
{
    private readonly IDispatcher _dispatcher;

    public Dispatcher(IDispatcher dispatcher) =>
        _dispatcher = dispatcher;

    public void Dispatch(Action action) =>
        _dispatcher.Dispatch(action);
}