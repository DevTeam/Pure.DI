namespace Build.Benchmarks;

using Immutype;

[ExcludeFromCodeCoverage]
[Target]
public readonly record struct Thresholds(
    string Type = "",
    string BaselineMethod = "",
    string BenchmarkMethod = "",
    double? WarningTimeRatio = null,
    double? WarningBytesAllocatedPerOperationRatio = null,
    double? ErrorTimeRatio = null,
    double? ErrorBytesAllocatedPerOperationRatio = null);