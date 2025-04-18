﻿namespace MAUIApp;

using IDispatcher = Microsoft.Maui.Dispatching.IDispatcher;

// ReSharper disable once ClassNeverInstantiated.Global
class Dispatcher(IDispatcher dispatcher)
    : Clock.ViewModels.IDispatcher
{
    public void Dispatch(Action action) =>
        dispatcher.Dispatch(action);
}