namespace Pure.DI.Benchmarks.Model;

using System.Diagnostics.CodeAnalysis;

public sealed class Service2 : IService2
{
    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    public Service2(
        IService3 service31,
        IService3 service32,
        IService3 service33,
        IService3 service34,
        IService3 service35)
    {
    }
}