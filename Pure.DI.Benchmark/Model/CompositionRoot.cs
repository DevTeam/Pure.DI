namespace Pure.DI.Benchmark.Model;

using System.Diagnostics.CodeAnalysis;

[SuppressMessage("ReSharper", "UnusedParameter.Local")]
public sealed class CompositionRoot : ICompositionRoot
{
    public CompositionRoot(IService1 service1, IService2 service21, IService2 service22, IService2 service23, IService3 service3)
    {
    }

    public bool Verify() => true;
}