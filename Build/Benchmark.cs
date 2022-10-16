using CoreHtmlToImage;
using HostApi;
using JetBrains.TeamCity.ServiceMessages.Write.Special;
// ReSharper disable StringLiteralTypo
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeTypeModifiers

class Benchmark
{
    private readonly Settings _settings;
    private readonly ICommandLineRunner _commandLineRunner;
    private readonly ITeamCityWriter _teamCityWriter;
    private readonly HtmlConverter _htmlConverter;

    private static readonly string[] Reports = {
        "Singleton",
        "Transient",
        "Func",
        "Array",
        "Enum",
        "ServiceProvider"
    };

    public Benchmark(
        Settings settings, 
        ICommandLineRunner commandLineRunner,
        ITeamCityWriter teamCityWriter,
        HtmlConverter htmlConverter)
    {
        _settings = settings;
        _commandLineRunner = commandLineRunner;
        _teamCityWriter = teamCityWriter;
        _htmlConverter = htmlConverter;
    }

    public void Run()
    {
        var benchmark = new DotNetRun()
            .WithProject(Path.Combine("Pure.DI.Benchmark", "Pure.DI.Benchmark.csproj"))
            .WithFramework("net6.0")
            .WithConfiguration(_settings.Configuration)
            .WithArgs("--", "--filter")
            .AddArgs(Reports.Select(filter => $"*{filter}*").ToArray());

        Assertion.Succeed(_commandLineRunner.Run(benchmark), "Benchmarking");

        var resultsPath = Path.Combine("BenchmarkDotNet.Artifacts", "results");
        foreach (var reportName in Reports)
        {
            var reportFileName = Path.Combine(resultsPath, $"Pure.DI.Benchmark.Benchmarks.{reportName}-report");
            var reportFileNameHtml = reportFileName + ".html";
            if (!File.Exists(reportFileNameHtml))
            {
                Warning($"The {reportFileNameHtml} file is missing.");
                continue;
            }

            _teamCityWriter.PublishArtifact($"{reportFileNameHtml} => .");
            var bytes = _htmlConverter.FromHtmlString(File.ReadAllText(reportFileNameHtml));
            var reportFileNameJpg = reportFileName + ".jpg";
            File.WriteAllBytes(reportFileNameJpg, bytes);
            _teamCityWriter.PublishArtifact($"{reportFileNameJpg} => .");
        }
    }
}