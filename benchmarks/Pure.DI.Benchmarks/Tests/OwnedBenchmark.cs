// ReSharper disable InconsistentlySynchronizedField
namespace Pure.DI.Benchmarks.Tests;

using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using Moq;

[SuppressMessage("Performance", "CA1822:Пометьте члены как статические")]
public class OwnedBenchmark
{
    private const int Count = 128;
    private static readonly IDisposable Disposable = Mock.Of<IDisposable>();
    
    [Benchmark(Baseline = true)]
    public List<IDisposable> ListAdd()
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
    public IOwned OwnedAdd()
    {
        var owned = new Owned();
        for (var i = 0; i < Count; i++)
        {
            owned.Add(Disposable);
        }
        
        return owned;
    }
}