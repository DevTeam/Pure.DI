namespace WinFormsAppNetCore;

using System;
using Clock.ViewModels;

// ReSharper disable once ClassNeverInstantiated.Global
internal class Dispatcher : IDispatcher
{
    private readonly Lazy<FormMain> _formMain;

    public Dispatcher(Lazy<FormMain> formMain) => 
        _formMain = formMain;

    public void Dispatch(Action action) =>
        _formMain.Value.Invoke(action);
}