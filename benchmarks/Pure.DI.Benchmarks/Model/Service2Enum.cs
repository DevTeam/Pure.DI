// ReSharper disable UnusedVariable
namespace Pure.DI.Benchmarks.Model;

using System.Diagnostics.CodeAnalysis;

public sealed class Service2Enum : IService2
{
    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    public Service2Enum(IEnumerable<IService3> services)
    {
        using var enumerator = services.GetEnumerator();
        enumerator.MoveNext();
    }
}