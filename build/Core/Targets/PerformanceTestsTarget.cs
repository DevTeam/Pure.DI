// ReSharper disable ClassNeverInstantiated.Global

namespace Build.Core.Targets;

using Benchmarks;

class PerformanceTestsTarget(
    Settings settings,
    Commands commands,
    Env env,
    FileSystem fileSystem,
    Json json,
    Analyzer analyzer)
    : IInitializable, ITarget<bool>
{
    private static readonly string[] Filters =
    [
        "Pure.DI.Benchmarks.Tests.*",
        "Pure.DI.Benchmarks.Benchmarks.*.Test*"
    ];

    public Task InitializeAsync(CancellationToken cancellationToken) => commands.RegisterAsync(
        this, "Performance tests", "performance", "perf");

    public async Task<bool> RunAsync(CancellationToken cancellationToken)
    {
        var solutionDirectory = env.GetPath(PathType.SolutionDirectory);
        var logsDirectory = Path.Combine(solutionDirectory, ".logs");
        Directory.CreateDirectory(logsDirectory);
        var tempDirectory = env.GetPath(PathType.TempDirectory);
        Directory.CreateDirectory(tempDirectory);
        var benchmarks = new List<Benchmark>();
        try
        {
            await new DotNetRun()
                .WithProject(Path.Combine("benchmarks", "Pure.DI.Benchmarks", "Pure.DI.Benchmarks.csproj"))
                .WithConfiguration(settings.Configuration)
                .WithArgs(
                    // Add Json exporter
                    "--exporters", "JSON",
                    "--artifacts", tempDirectory,
                    "--", "--filter")
                .AddArgs(Filters.Select(filter => filter).ToArray())
                .RunAsync(cancellationToken: cancellationToken).EnsureSuccess();

            foreach (var reportName in fileSystem.EnumerateFiles(tempDirectory, "*.json", SearchOption.AllDirectories))
            {
                await using var benchmarksJsonStream = fileSystem.OpenRead(reportName);
                if (json.TryDeserialize<BenchmarksDto>(benchmarksJsonStream) is {} dto)
                {
                    benchmarks.AddRange(dto.Benchmarks);
                }
            }
        }
        finally
        {
            Directory.Delete(tempDirectory, true);
        }

        var baseThreshold = new Thresholds()
            .WithWarningTimeRatio(1.4)
            .WithWarningBytesAllocatedPerOperationRatio(1.00)
            .WithErrorTimeRatio(1.5)
            .WithErrorBytesAllocatedPerOperationRatio(1.02);

        var rootThreshold = baseThreshold
            .WithBaselineMethod("TestHandCoded")
            .WithBenchmarkMethod("TestPureDIByCR");

        var resolveThreshold = baseThreshold
            .WithBaselineMethod("TestHandCoded")
            .WithBenchmarkMethod("TestPureDI");

        var objectResolveThreshold = baseThreshold
            .WithBaselineMethod("TestHandCoded")
            .WithBenchmarkMethod("TestPureDINonGeneric");

        return analyzer.Analyze(
            benchmarks,

            // AddDisposable
            new Thresholds(
                    "Pure.DI.Benchmarks.Tests.AddDisposableBenchmark",
                    "Baseline",
                    "AddDisposable")
                .WithWarningBytesAllocatedPerOperationRatio(1.1)
                .WithErrorBytesAllocatedPerOperationRatio(1.1),

            // Resolve
            new Thresholds(
                    "Pure.DI.Benchmarks.Tests.ResolveBenchmark",
                    "Baseline",
                    "Resolve")
                .WithWarningTimeRatio(0.48)
                .WithErrorTimeRatio(0.53),

            // Transient
            rootThreshold
                .WithType("Pure.DI.Benchmarks.Benchmarks.Transient"),
            resolveThreshold
                .WithType("Pure.DI.Benchmarks.Benchmarks.Transient"),
            objectResolveThreshold
                .WithType("Pure.DI.Benchmarks.Benchmarks.Transient"),

            // Singleton
            rootThreshold
                .WithType("Pure.DI.Benchmarks.Benchmarks.Singleton"),
            resolveThreshold
                .WithType("Pure.DI.Benchmarks.Benchmarks.Singleton"),
            objectResolveThreshold
                .WithType("Pure.DI.Benchmarks.Benchmarks.Singleton"),

            // Func
            rootThreshold
                .WithType("Pure.DI.Benchmarks.Benchmarks.Func"),
            resolveThreshold
                .WithType("Pure.DI.Benchmarks.Benchmarks.Func"),
            objectResolveThreshold
                .WithType("Pure.DI.Benchmarks.Benchmarks.Func"),

            // Enum
            rootThreshold
                .WithType("Pure.DI.Benchmarks.Benchmarks.Enum"),
            resolveThreshold
                .WithType("Pure.DI.Benchmarks.Benchmarks.Enum"),
            objectResolveThreshold
                .WithType("Pure.DI.Benchmarks.Benchmarks.Enum"),

            // Array
            rootThreshold
                .WithType("Pure.DI.Benchmarks.Benchmarks.Array"),
            resolveThreshold
                .WithType("Pure.DI.Benchmarks.Benchmarks.Array"),
            objectResolveThreshold
                .WithType("Pure.DI.Benchmarks.Benchmarks.Array"));
    }
}