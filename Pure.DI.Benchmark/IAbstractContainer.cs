namespace Pure.DI.Benchmark
{
    using System;
    using IoC;

    public interface IAbstractContainer<out TActualContainer>: IDisposable
    {
        [CanBeNull] TActualContainer TryCreate();

        void Register(Type contractType, Type implementationType, AbstractLifetime lifetime = AbstractLifetime.Transient, string name = null);
    }
}
