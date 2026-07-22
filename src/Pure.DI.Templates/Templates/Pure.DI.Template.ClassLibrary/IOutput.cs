namespace _PureDIProjectName_;

/// <summary>
/// Abstracts a text output sink so that consumers do not depend on the console directly.
/// </summary>
public interface IOutput
{
    /// <summary>
    /// Writes a line of text followed by a line terminator.
    /// </summary>
    /// <param name="line">The text to write, or <see langword="null"/> to write only a line terminator.</param>
    void WriteLine(string? line);
}