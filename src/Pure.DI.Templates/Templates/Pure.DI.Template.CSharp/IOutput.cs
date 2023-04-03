namespace _PureDIProjectName_;

/// <summary>
/// Abstract output.
/// </summary>
internal interface IOutput
{
    /// <summary>Writes a string to the text stream, followed by a line terminator.</summary>
    /// <param name="line">The string to write. If <paramref name="line" /> is <see langword="null" />, only the line terminator is written.</param>
    void WriteLine(string? line);
}