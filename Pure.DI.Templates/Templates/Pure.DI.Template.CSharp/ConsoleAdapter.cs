namespace _PureDIProjectName_
{
    internal class ConsoleAdapter: IInput, IOutput
    {
        public string? ReadLine() => System.Console.In.ReadLine();

        public void WriteLine(string? line) => System.Console.Out.WriteLine(line);
    }
}