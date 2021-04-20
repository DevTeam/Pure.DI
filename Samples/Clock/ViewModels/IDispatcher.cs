namespace Clock.ViewModels
{
    using System;

    // ReSharper disable once InconsistentNaming
    public interface IDispatcher
    {
        void Dispatch(Action action);
    }
}
