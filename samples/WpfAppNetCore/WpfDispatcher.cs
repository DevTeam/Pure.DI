namespace WpfAppNetCore;

internal class WpfDispatcher : IDispatcher
{
    public void Dispatch(Action action) =>
        Application.Current?.Dispatcher?.Invoke(action);
}