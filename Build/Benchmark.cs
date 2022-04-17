using HostApi;
using JetBrains.TeamCity.ServiceMessages.Write.Special;
// ReSharper disable StringLiteralTypo

class Benchmark
{
    private readonly Settings _settings;
    private readonly ICommandLineRunner _commandLineRunner;
    private readonly ITeamCityWriter _teamCityWriter;
    private static readonly string[] Reports = {
        "Singleton",
        "Transient",
        "Func",
        "Array",
        "Enum"
    };

    public Benchmark(
        Settings settings, 
        ICommandLineRunner commandLineRunner,
        ITeamCityWriter teamCityWriter)
    {
        _settings = settings;
        _commandLineRunner = commandLineRunner;
        _teamCityWriter = teamCityWriter;
    }

    public void Run()
    {
        var benchmark = new DotNetRun()
            .WithProject(Path.Combine("Pure.DI.Benchmark", "Pure.DI.Benchmark.csproj"))
            .WithFramework("net6.0")
            .WithConfiguration(_settings.configuration)
            .WithArgs("--", "--filter")
            .AddArgs(Reports.Select(filter => $"*{filter}*").ToArray());

        Assertion.Succeed(_commandLineRunner.Run(benchmark), "Benchmarking");

        var resultsPath = Path.Combine("BenchmarkDotNet.Artifacts", "results");
        var baseRenderCmd = new CommandLine(Path.Combine("Tools", "wkhtmltoimage", "wkhtmltoimage.exe"));
        var renderTasks =
            from reportName in Reports
            let reportFileName = Path.Combine(resultsPath, $"Pure.DI.Benchmark.Benchmarks.{reportName}-report")
            let reportFileNameHtml = reportFileName + ".html"
            let reportFileNameJpg  = reportFileName + ".jpg"
            let renderCmd = baseRenderCmd.AddArgs(reportFileNameHtml, reportFileNameJpg)
            select _commandLineRunner.RunAsync(renderCmd);

        _teamCityWriter.PublishArtifact("BenchmarkDotNet.Artifacts/results/*.* => .");
        var renderResults = Task.WhenAll(renderTasks).Result;
        Assertion.Succeed(renderResults, "Result rendering");
    }
}