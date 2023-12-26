namespace AvaloniaApp;

using Clock.ViewModels;

// ReSharper disable once ClassNeverInstantiated.Global
internal class Dispatcher(Avalonia.Threading.IDispatcher dispatcher): IDispatcher
{
    public void Dispatch(Action action) => dispatcher.Post(action);
}