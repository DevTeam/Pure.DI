namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;

    internal interface ITracer
    {
        IEnumerable<Dependency[]> Paths { get; }
        
        IDisposable RegisterResolving(Dependency dependency);

        void Save();

        void Reset();
    }
}
