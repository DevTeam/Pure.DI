namespace Build.Benchmarks;

[ExcludeFromCodeCoverage]
[Immutype.Target]
public readonly record struct Thresholds(
    string Type = "",
    string BaselineMethod = "",
    string BenchmarkMethod = "",
    double? WarningTimeRatio = default,
    double? WarningBytesAllocatedPerOperationRatio = default,
    double? ErrorTimeRatio = default,
    double? ErrorBytesAllocatedPerOperationRatio = default);
