namespace _PureDIProjectName_;

public class ConsoleAdapter: IInput, IOutput
{
    public ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken = default) =>
        Console.In.ReadLineAsync(cancellationToken);

    public Task WriteLineAsync(string line = "", CancellationToken cancellationToken = default) =>
        Console.Out.WriteLineAsync(line.AsMemory(), cancellationToken);
}