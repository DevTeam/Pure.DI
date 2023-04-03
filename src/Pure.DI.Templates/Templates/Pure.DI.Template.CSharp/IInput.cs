namespace _PureDIProjectName_;

/// <summary>
/// Abstract input.
/// </summary>
internal interface IInput
{
    // ReSharper disable once UnusedMethodReturnValue.Global
    /// <summary>Reads a line of characters from the text reader and returns the data as a string.</summary>
    /// <returns>The next line from the reader, or <see langword="null" /> if all characters have been read.</returns>
    string? ReadLine();
}