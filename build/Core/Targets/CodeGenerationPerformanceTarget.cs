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
    private static readonly ProfileDefinition[] Profiles =
    [
        new("AllPatterns", 1311, 28, 2560),
        new("Bindings", 2400, 38, 0),
        new("Roots", 160, 512, 0),
        new("Declarations", 0, 1, 1600),
        new("FactoriesAndTags", 400, 1, 0),
        new("ScopesAndAccumulators", 400, 1, 0),
        new("MultiTypeLifetimes", 2400, 1, 0),
        new("AttributeApis", 2400, 1, 0),
        new("GenericsAndVariants", 400, 1, 0)
    ];

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
        var runReportsDirectory = Path.Combine(
            reportsDirectory,
            $"{DateTime.UtcNow:yyyyMMdd-HHmmssfff}");
        var sessionDirectory = Path.Combine(
            logsDirectory,
            $"code-generation-performance-{Guid.NewGuid():N}");
        var patternPath = Path.Combine(sessionDirectory, "patterns.xml");
        var templatePath = Path.Combine(
            solutionDirectory,
            "samples",
            "HugeComposition",
            "Composition.tt");

        Directory.CreateDirectory(runReportsDirectory);
        Directory.CreateDirectory(sessionDirectory);
        try
        {
            var toolsPath = GetDotTraceToolsPath();
            var dotTracePath = Path.Combine(toolsPath, "dottrace.exe");
            var reporterPath = Path.Combine(toolsPath, "Reporter.exe");
            EnsureFileExists(dotTracePath);
            EnsureFileExists(reporterPath);
            EnsureFileExists(templatePath);

            await CreatePatternsAsync(patternPath, cancellationToken);

            await RunAsync(
                new CommandLine("dotnet", "tool", "restore")
                    .WithWorkingDirectory(solutionDirectory),
                "Local tool restore",
                cancellationToken);

            await RunAsync(
                new CommandLine(
                    "dotnet", "build", projectPath, "--configuration", settings.Configuration, "--no-restore", "--nologo", "--verbosity", "minimal")
                    .WithWorkingDirectory(solutionDirectory),
                "HugeComposition preparation",
                cancellationToken);

            foreach (var profile in Profiles)
            {
                var profileSessionDirectory = Path.Combine(sessionDirectory, profile.Name);
                var sourcePath = Path.Combine(profileSessionDirectory, "Composition.Profile.cs");
                var snapshotPath = Path.Combine(profileSessionDirectory, "code-generation.dtp");
                var reportPath = Path.Combine(runReportsDirectory, $"{profile.Name}.xml");
                Directory.CreateDirectory(profileSessionDirectory);

                await GenerateProfileAsync(
                    templatePath,
                    sourcePath,
                    profile,
                    solutionDirectory,
                    cancellationToken);

                Summary("Profiling code generation profile ", profile.Name.WithColor(Color.Details));
                await RunAsync(
                    new CommandLine(
                        dotTracePath, "start", "--profiling-type=Sampling", "--profile-child", "--propagate-exit-code", "--no-check-for-updates", "--overwrite", $"--save-to={snapshotPath}", $"--work-dir={solutionDirectory}", GetDotNetPath(), "--", "build", projectPath, "--configuration", settings.Configuration, "--no-restore", "--no-dependencies", "--no-incremental", "--nologo", "--verbosity", "minimal", "-m:1", "-nodeReuse:false", "-p:UseSharedCompilation=false", $"-p:HugeCompositionSource={sourcePath}")
                        .WithWorkingDirectory(solutionDirectory),
                    $"dotTrace profiling ({profile.Name})",
                    cancellationToken);

                await RunAsync(
                    new CommandLine(
                        reporterPath, "--no-check-for-updates", "--add-process-info", "report", Path.Combine(profileSessionDirectory, "*.dtp"), $"--pattern={patternPath}", $"--save-to={reportPath}", "--overwrite", "--save-signature")
                        .WithWorkingDirectory(solutionDirectory),
                    $"dotTrace report generation ({profile.Name})",
                    cancellationToken,
                    () => IsValidReport(reportPath));

                EnsureValidReport(reportPath);
                Summary("Report: ", reportPath.WithColor(Color.Details));
            }

            return runReportsDirectory;
        }
        finally
        {
            if (Directory.Exists(sessionDirectory))
            {
                Directory.Delete(sessionDirectory, true);
            }
        }
    }

    private async Task GenerateProfileAsync(
        string templatePath,
        string sourcePath,
        ProfileDefinition profile,
        string workingDirectory,
        CancellationToken cancellationToken) =>
        await RunAsync(
            new CommandLine(
                GetDotNetPath(), "tool", "run", "t4", "--", $"--out={sourcePath}", $"--parameter=Profile={profile.Name}", $"--parameter=BindingCount={profile.BindingCount}", $"--parameter=RootCount={profile.RootCount}", $"--parameter=DeclarationCount={profile.DeclarationCount}", templatePath)
                .WithWorkingDirectory(workingDirectory),
            $"HugeComposition profile generation ({profile.Name})",
            cancellationToken);

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

    private static string GetDotTraceToolsPath() => GetAssemblyMetadataPath(ToolsPathMetadataName);

    private static string GetAssemblyMetadataPath(string metadataName)
    {
        var path = Assembly
            .GetExecutingAssembly()
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == metadataName)
            ?.Value;
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException(
                $"The package path '{metadataName}' was not embedded into the build application.");
        }

        return path;
    }

    private sealed record ProfileDefinition(
        string Name,
        int BindingCount,
        int RootCount,
        int DeclarationCount);

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
