namespace Pure.DI.Core;

interface ISetOfOptions<out T>
{
    bool IsStarted { get; }

    T Current { get; }

    bool MoveNext();

    void Reset();
}