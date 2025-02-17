namespace Build.Benchmarks;

using Immutype;

[ExcludeFromCodeCoverage]
[Target]
public readonly record struct MemoryStatistics(
    int Gen0Collections = 0,
    int Gen1Collections = 0,
    int Gen2Collections = 0,
    int TotalOperations = 0,
    int BytesAllocatedPerOperation = 0);