namespace WinFormsAppNetCore;

using Clock.ViewModels;

// ReSharper disable once ClassNeverInstantiated.Global
class Dispatcher(Lazy<FormMain> formMain) : IDispatcher
{
    public void Dispatch(Action action) =>
        formMain.Value.Invoke(action);
}