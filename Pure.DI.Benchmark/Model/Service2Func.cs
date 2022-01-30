namespace Pure.DI.Benchmark.Model;

using System.Diagnostics.CodeAnalysis;

public sealed class Service2Func : IService2
{
    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    public Service2Func(Func<IService3> service3Factory)
    {
        service3Factory();
        service3Factory();
        service3Factory();
        service3Factory();
    }
}