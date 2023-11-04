using System;

namespace _PureDIProjectName_;

internal class ConsoleAdapter: IInput, IOutput
{
    public string? ReadLine() => Console.ReadLine();

    public void WriteLine(string? line) => Console.WriteLine(line);
}