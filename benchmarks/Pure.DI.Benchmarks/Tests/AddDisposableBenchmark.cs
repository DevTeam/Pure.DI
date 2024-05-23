// ReSharper disable InconsistentlySynchronizedField
namespace Pure.DI.Benchmarks.Tests;

using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Moq;

[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[MemoryDiagnoser]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
public class AddDisposableBenchmark
{
    private const int Count = 128;
    private static readonly IDisposable Disposable = Mock.Of<IDisposable>();
    
    [Benchmark(Baseline = true)]
    public List<IDisposable> Baseline()
    {
        var list = new List<IDisposable>();
        for (var i = 0; i < Count; i++)
        {
            if (Disposable is IOwned)
            {
                continue;
            }

            list.Add(Disposable);
        }

        return list;
    }
    
    [Benchmark]
    public IDisposable AddDisposable()
    {
        var owned = new Owned();
        for (var i = 0; i < Count; i++)
        {
            owned.Add(Disposable);
        }
        
        return owned;
    }
}