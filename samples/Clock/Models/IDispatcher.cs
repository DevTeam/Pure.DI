namespace Clock.Models;

public interface IDispatcher
{
    void Dispatch(Action action);
}