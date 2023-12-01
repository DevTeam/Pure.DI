// ReSharper disable ClassNeverInstantiated.Global
namespace BlazorWebAssemblyApp;

using Clock.ViewModels;

internal class Dispatcher: IDispatcher
{
    public void Dispatch(Action action) => action();
}