using HostApi;
using NuGet.Versioning;

class Root
{
    private readonly ICommandLineRunner _commandLineRunner;
    private readonly Settings _settings;
    private readonly IProperties _properties;
    private readonly Build _build;
    private readonly Deploy _deploy;
    private readonly DeployTemplate _deployTemplate;
    private readonly Benchmark _benchmark;

    public Root(
        Settings settings,
        IProperties properties,
        ICommandLineRunner commandLineRunner,
        Build build,
        Deploy deploy,
        DeployTemplate deployTemplate,
        Benchmark benchmark)
    {
        _settings = settings;
        _properties = properties;
        _commandLineRunner = commandLineRunner;
        _build = build;
        _deploy = deploy;
        _deployTemplate = deployTemplate;
        _benchmark = benchmark;
    }

    public void Run()
    {
        var installedSdks = GetInstalledSdkVersions().ToHashSet();
        foreach (var versionRange in GetUnmatchedRanges(installedSdks, _settings.RequiredSdkRange).ToArray())
        {
            Error($"Required .NET SDK {versionRange} is not installed.");
            Environment.Exit(1);
        }

        switch (Property.Get(_properties, "target", "Build"))
        {
            case "Build":
                _build.Run();
                break;
            
            case "Deploy":
                _deploy.Run();
                break;
            
            case "DeployTemplate":
                _deployTemplate.Run();
                break;
            
            case "Benchmark":
                _benchmark.Run();
                break;
        }
    }
    
    private static IEnumerable<VersionRange> GetUnmatchedRanges(
        IReadOnlyCollection<NuGetVersion> versions,
        params VersionRange[] ranges) => 
        ranges.Where(requiredSdkVersionRange => versions.All(sdk => !requiredSdkVersionRange.Satisfies(sdk)));

    private IEnumerable<NuGetVersion> GetInstalledSdkVersions()
    {
        var lines = new List<Output>();
        _commandLineRunner.Run(new DotNetCustom("--info"), output => lines.Add(output));
        return lines
            .Where(output => !output.IsError)
            .Select(output => output.Line)
            .SkipWhile(line => !line.Contains(".NET SDKs installed:"))
            .Skip(1)
            .TakeWhile(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.TrimStart().Split(' ', 2))
            .Select(parts => parts.FirstOrDefault() ?? string.Empty)
            .Select(versionStr => NuGetVersion.TryParse(versionStr, out var version) ? version : default)
            .Where(version => version != default)
            .Select(version => version!);
    }
}