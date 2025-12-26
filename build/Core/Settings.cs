// ReSharper disable MemberCanBeMadeStatic.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Build.Core;

using System.Collections.Immutable;
using System.Xml.Linq;
using NuGet.Versioning;

[SuppressMessage("Performance", "CA1822:Mark members as static")]
class Settings
{
    private readonly Lazy<VersionRange> _versionRange;
    private readonly Lazy<NuGetVersion> _currentVersion;
    private readonly Properties _properties;

    public Settings(Properties properties, Versions versions, Env env)
    {
        _properties = properties;
        _versionRange = new Lazy<VersionRange>(() => GetVersionRange(env));
        _currentVersion = new Lazy<NuGetVersion>(() => GetVersion(versions));
        NuGetKey = properties["NuGetKey"];
        Tests = !bool.TryParse(properties["tests"], out var tests) || tests;
    }

    public VersionRange VersionRange => _versionRange.Value;

    public bool BuildServer { get; } = Environment.GetEnvironmentVariable("TEAMCITY_VERSION") is not null;

    public string Configuration => "Release";

    public NuGetVersion CurrentVersion => _currentVersion.Value;

    public NuGetVersion NextVersion
    {
        get
        {
            if (NuGetVersion.TryParse(_properties["version"], out var version))
            {
                WriteLine($"The next version has been overridden by {version}.", Color.Details);
                return version;
            }

            var currentVersion = _currentVersion.Value;
            return new NuGetVersion(currentVersion.Major, currentVersion.Minor, currentVersion.Patch + 1);
        }
    }

    public string NuGetKey { get; }

    public ImmutableArray<CodeAnalysis> CodeAnalysis { get; } =
    [
        new(new Version(4, 8, 0)),
        new(new Version(4, 3, 1))
    ];

    // Make sure that /Directory.Build.props has been updated.
    public int BaseDotNetFrameworkMajorVersion => 10;

    public string BaseDotNetFrameworkVersion => $"{BaseDotNetFrameworkMajorVersion}.0";

    public bool Tests { get; }

    private NuGetVersion GetVersion(Versions versions) =>
        versions.GetNext(new NuGetRestoreSettings("Pure.DI"), VersionRange, 0);

    private static VersionRange GetVersionRange(Env env)
    {
        var propsFile = Path.Combine(env.GetPath(PathType.SolutionDirectory), "Directory.Build.props");
        if (!File.Exists(propsFile))
        {
            Error($"Could not find the props file: {propsFile}");
            return VersionRange.Parse("*");
        }

        var doc = XDocument.Load(propsFile);
        var versionRangeText = doc.Descendants("VersionRange").FirstOrDefault()?.Value;

        // ReSharper disable once InvertIf
        if (string.IsNullOrWhiteSpace(versionRangeText))
        {
            Error($"Could not find 'VersionRange' in {propsFile}");
            return VersionRange.Parse("*");
        }

        return VersionRange.Parse(versionRangeText);
    }
}