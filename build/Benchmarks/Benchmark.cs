namespace Build.Benchmarks;

using Immutype;

[ExcludeFromCodeCoverage]
[Target]
public readonly record struct Benchmark(
    string Namespace = "",
    string Type = "",
    string Method = "",
    string MethodTitle = "",
    string Parameters = "",
    string FullName = "",
    Statistics Statistics = default,
    MemoryStatistics Memory = default);