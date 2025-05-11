namespace ConsoleApp;

internal interface IConsole
{
    bool IsKeyAvailable { get; }

    void Write(object? value);
}