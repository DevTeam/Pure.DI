namespace Pure.DI.Core
{
    using System;

    internal interface ITracer
    {
        IDisposable RegisterResolving(Dependency dependency);
    }
}
