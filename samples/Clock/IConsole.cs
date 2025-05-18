namespace Clock;

public interface IConsole
{
    bool KeyAvailable {get; }

    void WaitForKey();

    void Write(object? value);
}