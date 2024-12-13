namespace Build.Benchmarks;

[ExcludeFromCodeCoverage]
[Immutype.Target]
public readonly record struct Thresholds(
    string Type = "",
    string BaselineMethod = "",
    string BenchmarkMethod = "",
    double? WarningTimeRatio = null,
    double? WarningBytesAllocatedPerOperationRatio = null,
    double? ErrorTimeRatio = null,
    double? ErrorBytesAllocatedPerOperationRatio = null);