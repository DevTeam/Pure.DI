// ReSharper disable ClassNeverInstantiated.Global
namespace Build;

using System.CommandLine;
using System.CommandLine.Invocation;
using HostApi;
using JetBrains.TeamCity.ServiceMessages.Write.Special;

internal class BenchmarksTarget: Command, ITarget<int>
{
    private readonly Settings _settings;
    private readonly ITeamCityWriter _teamCityWriter;

    private static readonly string[] Reports =
    [
        "Transient",
        "Singleton",
        "Func",
        "Array",
        "Enum"
    ];

    public BenchmarksTarget(
        Settings settings, 
        ITeamCityWriter teamCityWriter)
        : base("benchmarks", "Runs benchmarks")
    {
        _settings = settings;
        _teamCityWriter = teamCityWriter;
        this.SetHandler(RunAsync);
        AddAlias("b");
    }
    
    public Task<int> RunAsync(InvocationContext ctx)
    {
        Info("Benchmarking");
        var solutionDirectory = Tools.GetSolutionDirectory();
        var logsDirectory = Path.Combine(solutionDirectory, ".logs");
        Directory.CreateDirectory(logsDirectory);
        var artifactsDirectory = Path.Combine(solutionDirectory, "benchmarks", "data");
        if (!_settings.BuildServer && Directory.Exists(artifactsDirectory))
        {
            Warning($"The directory \"{artifactsDirectory}\" exists, benchmarks are skipped. Delete this directory to re-run the benchmarks.");
        }
        else
        {
            Directory.CreateDirectory(artifactsDirectory);
        
            var benchmark = new DotNetRun()
                .WithProject(Path.Combine("benchmarks", "Pure.DI.Benchmarks", "Pure.DI.Benchmarks.csproj"))
                .WithConfiguration(_settings.Configuration)
                .WithArgs("--artifacts", artifactsDirectory, "--", "--filter")
                .AddArgs(Reports.Select(filter => $"*{filter}*").ToArray());

            benchmark.Run().Succeed("Benchmarking");
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
            _teamCityWriter.PublishArtifact($"{reportFileNameHtml} => .");
        }

        return Task.FromResult(0);
    }
}