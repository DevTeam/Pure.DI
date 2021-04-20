namespace Clock.ViewModels
{
    using System;

    internal class Dispatcher : IDispatcher
    {
        public void Dispatch(Action action) => action();
    }
}
