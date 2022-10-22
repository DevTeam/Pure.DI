namespace Pure.DI.Core;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class StdOut : IStdOut
{
    public void WriteLine(string info) => Console.WriteLine(info);
}