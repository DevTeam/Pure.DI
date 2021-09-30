namespace _PureDIProjectName_
{
    /// <summary>
    /// Represents an adapter for console read/write operations.
    /// </summary>
    internal class ConsoleAdapter: IInput, IOutput
    {
        /// <inheritdoc />
        public string? ReadLine() => System.Console.ReadLine();

        /// <inheritdoc />
        public void WriteLine(string? line) => System.Console.WriteLine(line);
    }
}