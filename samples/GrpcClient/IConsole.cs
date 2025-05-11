namespace GrpcClient;

internal interface IConsole
{
    bool IsKeyAvailable { get; }

    void Write(object? value);
}