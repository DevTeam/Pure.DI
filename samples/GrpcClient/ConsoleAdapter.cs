namespace GrpcClient;

internal class ConsoleAdapter : IConsole
{
    private readonly (int Left, int Top) _position = Console.GetCursorPosition();

    public bool IsKeyAvailable => Console.KeyAvailable;

    public void Write(object? value)
    {
        Console.CursorVisible = false;
        Console.SetCursorPosition(_position.Left, _position.Top);
        Console.Write(value);
    }
}