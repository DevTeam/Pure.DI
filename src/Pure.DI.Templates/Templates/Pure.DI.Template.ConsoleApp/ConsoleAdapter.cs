namespace _PureDIProjectName_;

/// <summary>
/// The default <see cref="IInput"/> and <see cref="IOutput"/> implementation backed by the system
/// <see cref="Console"/>. Swap this binding in the composition to redirect input and output, for example in tests.
/// </summary>
internal class ConsoleAdapter: IInput, IOutput
{
    /// <inheritdoc />
    public string? ReadLine() => Console.ReadLine();

    /// <inheritdoc />
    public void WriteLine(string? line) => Console.WriteLine(line);
}