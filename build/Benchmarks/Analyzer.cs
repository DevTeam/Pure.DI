namespace Build.Benchmarks;

internal class Analyzer
{
    public BuildStatus Analyze(IEnumerable<Benchmark> benchmarks, params Thresholds[] thresholds)
    {
        var status = BuildStatus.Succeed;
        var groupsByType = benchmarks.GroupBy(i => i.Type, StringComparer.InvariantCultureIgnoreCase);
        foreach (var groupByType in groupsByType)
        {
            var benchmarkType = groupByType.Key;
            var methods = groupByType.ToDictionary(i => i.Method, i => i, StringComparer.InvariantCultureIgnoreCase);
            foreach (var threshold in thresholds.Where(i => i.Type.Equals(benchmarkType, StringComparison.InvariantCultureIgnoreCase)))
            {
                if (!methods.TryGetValue(threshold.BenchmarkMethod, out var benchmark))
                {
                    Error($"Cannot find baseline method \"{threshold.BenchmarkMethod}\" in \"{benchmarkType}\".");
                    return BuildStatus.Fail;
                }
                
                if (!methods.TryGetValue(threshold.BaselineMethod, out var baselineBenchmark))
                {
                    Error($"Cannot find baseline method \"{threshold.BaselineMethod}\" in \"{benchmarkType}\".");
                    return BuildStatus.Fail;
                }

                status += CheckThreshold(
                    "time",
                    threshold.WarningTimeRatio,
                    threshold.ErrorTimeRatio,
                    baselineBenchmark.Statistics.Mean,
                    benchmark.Statistics.Mean,
                    benchmarkType,
                    threshold.BenchmarkMethod);
                
                status += CheckThreshold(
                    "bytes allocated per operation",
                    threshold.WarningBytesAllocatedPerOperationRatio,
                    threshold.ErrorBytesAllocatedPerOperationRatio,
                    baselineBenchmark.Memory.BytesAllocatedPerOperation,
                    benchmark.Memory.BytesAllocatedPerOperation,
                    benchmarkType,
                    threshold.BenchmarkMethod);
            }
        }
        
        return status;
    }
    
    private static BuildStatus CheckThreshold(string name, double? warningThreshold, double? errorThreshold, double baseline, double value, string benchmarkType, string benchmarkMethod)
    {
        var ratio = value / baseline;
        if (errorThreshold > double.Epsilon && ratio > errorThreshold)
        {
            Error(CreateMessage(name, errorThreshold, ratio, benchmarkType, benchmarkMethod, "must"));
            return BuildStatus.Fail;
        }
        
        // ReSharper disable once InvertIf
        if (warningThreshold > double.Epsilon && ratio > warningThreshold)
        {
            Warning(CreateMessage(name, warningThreshold, ratio, benchmarkType, benchmarkMethod, "could"));
            return BuildStatus.Warnings;
        }
        
        return BuildStatus.Succeed;
    }

    private static string CreateMessage(
        string name,
        double? thresholdRatio,
        double ratio,
        string benchmarkType,
        string benchmarkMethod,
        string verb) => 
        $"The {benchmarkType} {benchmarkMethod} method {verb} meet a {name} ratio threshold of {thresholdRatio}, but it is currently {ratio:0.##}.";
}
