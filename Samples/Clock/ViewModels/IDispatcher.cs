namespace Clock.ViewModels;

public interface IDispatcher
{
    void Dispatch(Action action);
}