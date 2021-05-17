// ReSharper disable InconsistentNaming
namespace WpfAppNetCore
{
    using System;
    using System.Windows;
    using Clock.ViewModels;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class Dispatcher: IDispatcher
    {
        public void Dispatch(Action action) => Application.Current?.Dispatcher?.Invoke(action);
    }
}
