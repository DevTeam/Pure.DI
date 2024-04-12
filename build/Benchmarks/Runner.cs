namespace Build.Benchmarks;

using System.Collections.Immutable;

internal class Runner(
    ICommandLineRunner commandLineRunner,
    Env env,
    FileSystem fileSystem,
    Json json)
{
    private const string JsonFilePostfix = "-report-full-compressed.json";
    private const string DefaultBenchmarkDotNetArtifactsPath = "BenchmarkDotNet.Artifacts";

    private static readonly ImmutableArray<string> DefaultArgs =
    [
        // Add Json exporter
        "--exporters", "JSON",

        // Use Memory Diagnoser to get GC stats
        "-m"
    ];

    public async Task<IReadOnlyCollection<Benchmark>> RunAsync(ICommandLine commandLine, Thresholds thresholds, CancellationToken cancellationToken)
    {
        var benchmarkNames = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        var benchmarksListResult = await commandLineRunner.RunAsync(
            commandLine.Customize(i => i.AddArgs("--list", "flat").WithShortName("Gets a list of benchmarks")),
            output =>
            {
                Trace(output.Line);
                if (output.Line.Any(ch => fileSystem.InvalidFileNameChars.Contains(ch)))
                {
                    return;
                }
                
                var className = output.Line;
                benchmarkNames.Add(className);
                WriteLine($"benchmark {className}", Color.Details);
            },
            cancellationToken);

        if (benchmarksListResult != 0 || !benchmarkNames.Any())
        {
            Error("Cannot get a list of benchmarks.");
            return Array.Empty<Benchmark>();
        }

        var reportsPath = Path.Combine(env.GetPath(PathType.BenchmarksResultDirectory), "Benchmarks");
        if (fileSystem.IsDirectoryExist(reportsPath))
        {
            fileSystem.DeleteDirectory(reportsPath, true);
        }

        var benchmarksExitCode = await commandLineRunner.RunAsync(commandLine.Customize(i => i.AddArgs(DefaultArgs)), default, cancellationToken);
        if (benchmarksExitCode != 0)
        {
            return Array.Empty<Benchmark>();
        }

        if (!fileSystem.IsDirectoryExist(DefaultBenchmarkDotNetArtifactsPath))
        {
            Error($"Cannot find benchmarks in {DefaultBenchmarkDotNetArtifactsPath}.");
            return Array.Empty<Benchmark>();
        }
        
        fileSystem.MoveDirectory(DefaultBenchmarkDotNetArtifactsPath, reportsPath);

        var resultsPath = Path.Combine(reportsPath, "results");
        var benchmarks = new List<Benchmark>(); 
        var reportFiles = benchmarkNames
            .SelectMany(benchmarkName => 
                fileSystem.EnumerateFiles(resultsPath, $"{benchmarkName}-report*", SearchOption.TopDirectoryOnly));
        foreach (var reportFile in reportFiles)
        {
            if (!reportFile.EndsWith(JsonFilePostfix))
            {
                continue;
            }

            await using var benchmarksJsonStream = fileSystem.OpenRead(reportFile);
            var benchmarksDto = json.TryDeserialize<BenchmarksDto>(benchmarksJsonStream);
            if (benchmarksDto != default)
            {
                benchmarks.AddRange(benchmarksDto.Benchmarks);
            }
        }
        
        if (benchmarks.Count == 0)
        {
            Warning($"Cannot find \"{Path.Combine(resultsPath, $"*{JsonFilePostfix}")}\" file to get benchmarks statistics.");
        }

        return benchmarks;
    }
    
    // ReSharper disable once MemberCanBePrivate.Global
    // ReSharper disable once ClassNeverInstantiated.Global
    public record BenchmarksDto(IReadOnlyCollection<Benchmark> Benchmarks);
}