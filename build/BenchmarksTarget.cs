// ReSharper disable ClassNeverInstantiated.Global

namespace Build;

internal class BenchmarksTarget(
    Settings settings,
    Commands commands,
    Paths paths,
    ITeamCityArtifactsWriter artifactsWriter)
    : IInitializable, ITarget<int>
{
    private static readonly string[] Reports =
    [
        "Transient",
        "Singleton",
        "Func",
        "Array",
        "Enum"
    ];

    public Task InitializeAsync() => commands.Register(
        this,
        "Runs benchmarks",
        "benchmarks",
        "bm");

    public Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var solutionDirectory = paths.SolutionDirectory;
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
            new DotNetRun()
                .WithProject(Path.Combine("benchmarks", "Pure.DI.Benchmarks", "Pure.DI.Benchmarks.csproj"))
                .WithConfiguration(settings.Configuration)
                .WithArgs("--artifacts", artifactsDirectory, "--", "--filter")
                .AddArgs(Reports.Select(filter => $"*{filter}*").ToArray())
                .Run()
                .Succeed("Benchmarking");
        }

        var index = 0;
        foreach (var reportName in Reports)
        {
            var reportFileName = Path.Combine(artifactsDirectory, "results", $"Pure.DI.Benchmarks.Benchmarks.{reportName}-report");
            var reportFileNameHtml = reportFileName + ".html";
            if (!File.Exists(reportFileNameHtml))
            {
                Warning($"The {reportFileNameHtml} file is missing.");
                continue;
            }

            var reportFile = Path.Combine(logsDirectory, $"{index++:00} {reportName}.html");
            File.Copy(reportFileNameHtml, reportFile, true);
            artifactsWriter.PublishArtifact($"{reportFileNameHtml} => .");
        }

        return Task.FromResult(0);
    }
}