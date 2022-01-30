// ReSharper disable UnusedVariable
namespace Pure.DI.Benchmark.Model;

using System.Diagnostics.CodeAnalysis;

public sealed class Service2Enum : IService2
{
    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    public Service2Enum(IEnumerable<IService3> services)
    {
        foreach (var service in services)
        {
        }
    }
}