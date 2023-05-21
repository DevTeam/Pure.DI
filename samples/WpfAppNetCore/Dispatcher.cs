namespace WpfAppNetCore;

using System.Windows;

// ReSharper disable once ClassNeverInstantiated.Global
internal class Dispatcher : IDispatcher
{
    public void Dispatch(Action action) => Application.Current?.Dispatcher?.Invoke(action);
}