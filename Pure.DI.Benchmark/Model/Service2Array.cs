namespace Pure.DI.Benchmark.Model;

using System.Diagnostics.CodeAnalysis;

public sealed class Service2Array : IService2
{
    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    public Service2Array(IService3[] services) { }
}