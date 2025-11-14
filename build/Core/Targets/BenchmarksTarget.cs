// ReSharper disable ClassNeverInstantiated.Global

namespace Build.Core.Targets;

class BenchmarksTarget(
    Settings settings,
    Commands commands,
    Env env,
    FileSystem fileSystem,
    ITeamCityArtifactsWriter artifactsWriter)
    : IInitializable, ITarget<int>
{
    private static readonly string[] Filters = ["Pure.DI.Benchmarks.Benchmarks.*"];

    public Task InitializeAsync(CancellationToken cancellationToken) => commands.RegisterAsync(
        this, "Run benchmarks", "benchmarks", "bm");

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var solutionDirectory = env.GetPath(PathType.SolutionDirectory);
        var logsDirectory = Path.Combine(solutionDirectory, ".logs");
        Directory.CreateDirectory(logsDirectory);
        var artifactsDirectory = Path.Combine(solutionDirectory, "benchmarks", "data");
        if (!settings.BuildServer && Directory.Exists(artifactsDirectory))
        {
            Warning($"The directory \"{artifactsDirectory}\" exists, benchmarks are skipped. Delete this directory to re-run the benchmarks.");
        }
        else
        {
            Directory.CreateDirectory(artifactsDirectory);
            await new DotNetRun()
                .WithProject(Path.Combine("benchmarks", "Pure.DI.Benchmarks", "Pure.DI.Benchmarks.csproj"))
                .WithConfiguration(settings.Configuration)
                .WithArgs("--artifacts", artifactsDirectory, "--", "--filter")
                .AddArgs(Filters.Select(filter => filter).ToArray())
                .RunAsync(cancellationToken: cancellationToken).EnsureSuccess();
        }

        var index = 0;
        var files = fileSystem.EnumerateFiles(artifactsDirectory, "*.csv", SearchOption.AllDirectories)
            .Concat(fileSystem.EnumerateFiles(artifactsDirectory, "*.html", SearchOption.AllDirectories));
        foreach (var originalReportFile in files )
        {
            var reportFile = $"{index++:00} {Path.GetFileName(originalReportFile).Replace("Pure.DI.Benchmarks.Benchmarks.", "").Replace("-report", "")}";
            reportFile = Path.Combine(logsDirectory, reportFile);
            File.Copy(originalReportFile, reportFile, true);
            artifactsWriter.PublishArtifact($"{originalReportFile} => .");
        }

        return 0;
    }
}