namespace WinFormsAppNetCore;

using System;
using Clock.ViewModels;

// ReSharper disable once ClassNeverInstantiated.Global
internal class Dispatcher(Lazy<FormMain> formMain) : IDispatcher
{
    public void Dispatch(Action action) =>
        formMain.Value.Invoke(action);
}