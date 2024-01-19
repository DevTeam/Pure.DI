namespace WinFormsApp;

using System;
using Clock.ViewModels;

// ReSharper disable once ClassNeverInstantiated.Global
internal class Dispatcher(Lazy<FormMain> formMain)
    : IDispatcher, IDisposable
{
    private volatile bool _disposed;

    public void Dispatch(Action action)
    {
        if (!_disposed)
        {
            formMain.Value.Invoke(action);
        }
    }

    public void Dispose() => _disposed = true;
}