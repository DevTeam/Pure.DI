namespace _PureDIProjectName_;

/// <summary>
/// The default <see cref="IInput"/> and <see cref="IOutput"/> implementation backed by the system
/// <see cref="global::System.Console"/>. Swap this binding in the composition to redirect input and output,
/// for example in tests.
/// </summary>
/// <remarks>
/// The type is public because the composition is packed into the library package and compiled in the consumer
/// project, which can only refer to public members of the library.
/// </remarks>
public class ConsoleAdapter : IInput, IOutput
{
    /// <inheritdoc />
    public string? ReadLine() => global::System.Console.ReadLine();

    /// <inheritdoc />
    public void WriteLine(string? line) => global::System.Console.WriteLine(line);
}
