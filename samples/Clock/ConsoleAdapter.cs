namespace Clock;

public class ConsoleAdapter : IConsole
{
    private readonly Lazy<(int Left, int Top)> _position = new(() => (Console.CursorLeft, Console.CursorTop));

    public bool KeyAvailable => Console.KeyAvailable;

    public void WaitForKey() => Console.ReadKey(true);

    public void Write(object? value)
    {
        Console.CursorVisible = false;
        Console.SetCursorPosition(_position.Value.Left, _position.Value.Top);
        Console.Write(value);
    }
}