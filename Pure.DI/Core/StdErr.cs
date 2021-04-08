namespace Pure.DI.Core
{
    using System;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class StdErr : IStdErr
    {
        public void WriteErrorLine(string info) => Console.Error.WriteLine(info);
    }
}