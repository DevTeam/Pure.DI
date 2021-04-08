namespace Pure.DI.Core
{
    using System;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class StdOut : IStdOut
    {
        public void WriteLine(string info) => Console.WriteLine(info);
    }
}