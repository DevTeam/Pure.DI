namespace Pure.DI.Core
{
    using System;

    internal interface ITracer
    {
        IDisposable RegisterResolving(TypeResolveDescription typeResolveDescription);
    }
}
