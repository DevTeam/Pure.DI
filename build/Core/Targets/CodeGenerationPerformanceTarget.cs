// ReSharper disable ClassNeverInstantiated.Global

namespace Build.Core.Targets;

using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Linq;

class CodeGenerationPerformanceTarget(
    Settings settings,
    Commands commands,
    Env env,
    ICommandLineRunner commandLineRunner)
    : IInitializable, ITarget<string>
{
    private const string ToolsPathMetadataName = "DotTraceToolsPath";

    public Task InitializeAsync(CancellationToken cancellationToken) => commands.RegisterAsync(
        this,
        "Profile Pure.DI code generation",
        "codegen-performance",
        "codegen-perf",
        "cgp");

    public async Task<string> RunAsync(CancellationToken cancellationToken)
    {
        var solutionDirectory = env.GetPath(PathType.SolutionDirectory);
        var projectPath = Path.Combine(
            solutionDirectory,
            "samples",
            "HugeComposition",
            "HugeComposition.csproj");
        var logsDirectory = Path.Combine(solutionDirectory, ".logs");
        var reportsDirectory = Path.Combine(logsDirectory, "code-generation-performance");
        var reportPath = Path.Combine(
            reportsDirectory,
            $"{DateTime.UtcNow:yyyyMMdd-HHmmssfff}.xml");
        var sessionDirectory = Path.Combine(
            logsDirectory,
            $"code-generation-performance-{Guid.NewGuid():N}");
        var snapshotPath = Path.Combine(sessionDirectory, "code-generation.dtp");
        var patternPath = Path.Combine(sessionDirectory, "patterns.xml");

        Directory.CreateDirectory(reportsDirectory);
        Directory.CreateDirectory(sessionDirectory);
        try
        {
            var toolsPath = GetDotTraceToolsPath();
            var dotTracePath = Path.Combine(toolsPath, "dottrace.exe");
            var reporterPath = Path.Combine(toolsPath, "Reporter.exe");
            EnsureFileExists(dotTracePath);
            EnsureFileExists(reporterPath);

            await CreatePatternsAsync(patternPath, cancellationToken);

            await RunAsync(
                new CommandLine(
                    "dotnet",
                    [
                        "build",
                        projectPath,
                        "--configuration", settings.Configuration,
                        "--no-restore",
                        "--nologo",
                        "--verbosity", "minimal"
                    ])
                    .WithWorkingDirectory(solutionDirectory),
                "HugeComposition preparation",
                cancellationToken);

            Summary(
                "Profiling code generation for ",
                Path.GetRelativePath(solutionDirectory, projectPath).WithColor(Color.Details));

            await RunAsync(
                new CommandLine(
                    dotTracePath,
                    [
                        "start",
                        "--profiling-type=Sampling",
                        "--profile-child",
                        "--propagate-exit-code",
                        "--no-check-for-updates",
                        "--overwrite",
                        $"--save-to={snapshotPath}",
                        $"--work-dir={solutionDirectory}",
                        GetDotNetPath(),
                        "--",
                        "build",
                        projectPath,
                        "--configuration", settings.Configuration,
                        "--no-restore",
                        "--no-dependencies",
                        "--no-incremental",
                        "--nologo",
                        "--verbosity", "minimal",
                        "-m:1",
                        "-nodeReuse:false",
                        "-p:UseSharedCompilation=false"
                    ])
                    .WithWorkingDirectory(solutionDirectory),
                "dotTrace profiling",
                cancellationToken);

            await RunAsync(
                new CommandLine(
                    reporterPath,
                    [
                        "--no-check-for-updates",
                        "--add-process-info",
                        "report",
                        Path.Combine(sessionDirectory, "*.dtp"),
                        $"--pattern={patternPath}",
                        $"--save-to={reportPath}",
                        "--overwrite",
                        "--save-signature"
                    ])
                    .WithWorkingDirectory(solutionDirectory),
                "dotTrace report generation",
                cancellationToken,
                () => IsValidReport(reportPath));

            EnsureValidReport(reportPath);
            Summary("Report: ", reportPath.WithColor(Color.Details));
            return reportPath;
        }
        finally
        {
            if (Directory.Exists(sessionDirectory))
            {
                Directory.Delete(sessionDirectory, true);
            }
        }
    }

    private async Task RunAsync(
        ICommandLine commandLine,
        string operation,
        CancellationToken cancellationToken,
        Func<bool>? acceptNonZeroExitCode = null)
    {
        var output = new StringBuilder();
        var result = await commandLineRunner.RunAsync(
            commandLine,
            line => {
                output.AppendLine(line.Line);
                line.Handled = true;
            },
            cancellationToken);

        if (result.ExitCode != 0 && acceptNonZeroExitCode?.Invoke() != true)
        {
            throw new InvalidOperationException(
                $"{operation} failed with exit code {result.ExitCode}.{Environment.NewLine}{output}");
        }
    }

    private static string GetDotTraceToolsPath()
    {
        var path = Assembly
            .GetExecutingAssembly()
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == ToolsPathMetadataName)
            ?.Value;
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException(
                "The JetBrains.dotTrace.CommandLineTools package path was not embedded into the build application.");
        }

        return path;
    }

    private static string GetDotNetPath()
    {
        if (Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") is { Length: > 0 } hostPath
            && File.Exists(hostPath))
        {
            return hostPath;
        }

        foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? "")
                     .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var path = Path.Combine(directory, "dotnet.exe");
            if (File.Exists(path))
            {
                return path;
            }
        }

        throw new FileNotFoundException("Unable to find dotnet.exe for dotTrace profiling.");
    }

    private static async Task CreatePatternsAsync(
        string path,
        CancellationToken cancellationToken)
    {
        var patterns = new XDocument(
            new XElement(
                "Patterns",
                new XElement(
                    "Pattern",
                    new XAttribute("PrintCallstacks", "Full"),
                    @"Pure\.DI\.SourceGenerator\..*"),
                new XElement(
                    "Pattern",
                    new XAttribute("PrintCallstacks", "Full"),
                    @"Pure\.DI\.Core\..*")));
        await using var stream = File.Create(path);
        await patterns.SaveAsync(stream, SaveOptions.None, cancellationToken);
    }

    private static void EnsureFileExists(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Required dotTrace command-line tool was not found.", path);
        }
    }

    private static void EnsureValidReport(string path)
    {
        if (!IsValidReport(path))
        {
            throw new InvalidOperationException(
                "dotTrace Reporter did not create a valid XML report containing Pure.DI functions.");
        }
    }

    private static bool IsValidReport(string path)
    {
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            var report = XDocument.Load(path);
            return report.Root?.Name.LocalName == "Report"
                   && report
                       .Descendants("Function")
                       .Any(function => function.Attribute("FQN")?.Value.StartsWith("Pure.DI.", StringComparison.Ordinal) == true);
        }
        catch (XmlException)
        {
            return false;
        }
    }
}
