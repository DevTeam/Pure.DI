namespace Pure.DI.Core
{
    using System;

    internal class StdOut : IStdOut
    {
        public void WriteLine(string info) => Console.WriteLine(info);
    }
}