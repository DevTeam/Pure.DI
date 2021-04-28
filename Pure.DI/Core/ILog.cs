namespace Pure.DI.Core
{
    using System;

    internal interface ILog<T>
    {
        void Trace(Func<string[]> messageFactory);

        void Info(Func<string[]> messageFactory);

        void Warning(params string[] warning);

        void Error(params string[] error);
    }
}
