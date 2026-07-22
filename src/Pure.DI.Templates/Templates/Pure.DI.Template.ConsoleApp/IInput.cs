namespace _PureDIProjectName_;

/// <summary>
/// Abstracts a source of text input so that consumers do not depend on the console directly.
/// </summary>
internal interface IInput
{
    /// <summary>
    /// Reads the next line of input.
    /// </summary>
    /// <returns>The line read, or <see langword="null"/> when no more input is available.</returns>
    string? ReadLine();
}