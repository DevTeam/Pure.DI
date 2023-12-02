// ReSharper disable InconsistentNaming
namespace Pure.DI.Benchmarks.Model;

public sealed class Service3v2 : IService3
{
    public Service3v2(IService4 service41, IService4 service42)
    {
    }
}