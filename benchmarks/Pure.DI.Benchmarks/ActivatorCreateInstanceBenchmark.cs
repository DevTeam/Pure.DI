namespace Pure.DI.Benchmarks;

using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using Model;

[SuppressMessage("Performance", "CA1822:Mark members as static")]
public class ActivatorCreateInstanceBenchmark
{
    [Benchmark(Description = "Activator.CreateInstance")]
    public CompositionRoot ActivatorCreateInstance() =>
        CreateInstance<CompositionRoot>(
            CreateInstance<Service1>(
                CreateInstance<Service2>(
                    CreateInstance<Service3>(),
                    CreateInstance<Service3>(),
                    CreateInstance<Service3>(),
                    CreateInstance<Service3>(),
                    CreateInstance<Service3>())),
            CreateInstance<Service2>(
                CreateInstance<Service3>(),
                CreateInstance<Service3>(),
                CreateInstance<Service3>(),
                CreateInstance<Service3>(),
                CreateInstance<Service3>()),
            CreateInstance<Service2>(
                CreateInstance<Service3>(),
                CreateInstance<Service3>(),
                CreateInstance<Service3>(),
                CreateInstance<Service3>(),
                CreateInstance<Service3>()),
            CreateInstance<Service2>(
                CreateInstance<Service3>(),
                CreateInstance<Service3>(),
                CreateInstance<Service3>(),
                CreateInstance<Service3>(),
                CreateInstance<Service3>()),
            CreateInstance<Service3>());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T CreateInstance<T>(params object[] args) =>
        (T)Activator.CreateInstance(typeof(T), args)!;
}