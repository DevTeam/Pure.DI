namespace Clock;

public interface IDispatcher
{
    void Dispatch(Action action);
}