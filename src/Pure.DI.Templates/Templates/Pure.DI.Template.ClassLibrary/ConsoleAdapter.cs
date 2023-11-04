using System;

namespace _PureDIProjectName_;

public class ConsoleAdapter: IInput, IOutput
{
    public string? ReadLine() => Console.ReadLine();

    public void WriteLine(string? line) => Console.WriteLine(line);
}