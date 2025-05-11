namespace MAUIReactorApp;

internal class MauiDispatcher(Microsoft.Maui.Dispatching.IDispatcher dispatcher)
    : Clock.IDispatcher
{
    public void Dispatch(Action action) =>
        dispatcher.Dispatch(action);
}