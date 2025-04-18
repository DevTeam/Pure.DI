// ReSharper disable ClassNeverInstantiated.Global
namespace Clock.Models;

class Dispatcher : IDispatcher
{
    public void Dispatch(Action action) => action();
}