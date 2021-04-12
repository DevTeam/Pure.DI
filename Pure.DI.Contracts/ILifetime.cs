namespace Pure.DI
{
    using System;

    public interface ILifetime<T>
    {
        T Resolve(Func<T> factory);
    }
}