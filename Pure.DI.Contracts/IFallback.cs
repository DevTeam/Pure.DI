namespace Pure.DI
{
    using System;

    public interface IFallback
    {
        object Resolve(Type type, object tag);
    }
}
