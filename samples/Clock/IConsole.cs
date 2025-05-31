namespace Clock;

public interface IConsole
{
    bool KeyAvailable {get; }

    void Write(object? value);
}