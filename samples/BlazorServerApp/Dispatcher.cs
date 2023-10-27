namespace BlazorServerApp;

using System.Diagnostics.CodeAnalysis;
using Clock.ViewModels;

[SuppressMessage("Usage", "BL0006:Do not use RenderTree types")]
internal class Dispatcher: IDispatcher
{
    public void Dispatch(Action action) =>
        action();
}