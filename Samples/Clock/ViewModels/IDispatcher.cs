namespace Clock.ViewModels
{
    using System;

    public interface IDispatcher
    {
        void Dispatch(Action action);
    }
}
