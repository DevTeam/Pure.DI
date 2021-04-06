namespace Pure.DI.Core
{
    using System;

    internal class StdErr : IStdErr
    {
        public void WriteErrorLine(string info) => Console.Error.WriteLine(info);
    }
}