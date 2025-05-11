namespace WinFormsAppNetCore;

internal class WinFormsDispatcher(Func<FormMain> formProvider)
    : IDispatcher
{
    public void Dispatch(Action action)
    {
        if (formProvider() is { IsDisposed: false, InvokeRequired: true } form)
        {
            form.Invoke(action);
        }
    }
}