namespace Build.Benchmarks;

[ExcludeFromCodeCoverage]
[Immutype.Target]
public readonly record struct Benchmark(
    string Namespace = "",
    string Type = "",
    string Method = "",
    string MethodTitle = "",
    string Parameters = "",
    string FullName = "",
    Statistics Statistics = default,
    MemoryStatistics Memory = default);
