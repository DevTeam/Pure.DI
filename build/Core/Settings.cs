// ReSharper disable MemberCanBeMadeStatic.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Build.Core;

using System.Collections.Immutable;
using System.Xml.Linq;
using NuGet.Versioning;

[SuppressMessage("Performance", "CA1822:Mark members as static")]
class Settings
{
    private readonly Lazy<XDocument> _buildPropsDoc;
    private readonly Lazy<VersionRange> _versionRange;
    private readonly Lazy<NuGetVersion> _currentVersion;
    private readonly Lazy<NuGetVersion> _nextVersion;
    private readonly Lazy<int> _baseDotNetFrameworkMajorVersion;
    private readonly Properties _properties;
    private readonly Versions _versions;
    private readonly Env _env;

    public Settings(Properties properties, Versions versions, Env env)
    {
        _properties = properties;
        _versions = versions;
        _env = env;
        _buildPropsDoc = new Lazy<XDocument>(GetBuildPropsDoc);
        _versionRange = new Lazy<VersionRange>(() => VersionRange.Parse(TryGetBuildProperty("VersionRange") ?? "*"));
        _currentVersion = new Lazy<NuGetVersion>(GetCurrentVersion);
        _nextVersion = new Lazy<NuGetVersion>(GetNextVersion);
        _baseDotNetFrameworkMajorVersion =new Lazy<int>(GetBaseTargetFrameworkMajorVersion);
        NuGetKey = properties["NuGetKey"];
        Tests = !bool.TryParse(properties["tests"], out var tests) || tests;
    }

    public VersionRange VersionRange => _versionRange.Value;

    public NuGetVersion CurrentVersion => _currentVersion.Value;

    public NuGetVersion NextVersion => _nextVersion.Value;

    public bool BuildServer { get; } = Environment.GetEnvironmentVariable("TEAMCITY_VERSION") is not null;

    public string Configuration => "Release";

    public string NuGetKey { get; }

    public ImmutableArray<CodeAnalysis> CodeAnalysis { get; } =
    [
        new(new Version(4, 8, 0)),
        new(new Version(4, 3, 1))
    ];

    // Make sure that /Directory.Build.props has been updated.
    public int BaseDotNetFrameworkMajorVersion => _baseDotNetFrameworkMajorVersion.Value;

    public string BaseDotNetFrameworkVersion => $"{BaseDotNetFrameworkMajorVersion}.0";

    public bool Tests { get; }

    private NuGetVersion GetCurrentVersion() =>
        _versions.GetCurrent(new NuGetRestoreSettings("Pure.DI"));

    private NuGetVersion GetNextVersion()
    {
        // ReSharper disable once InvertIf
        if (NuGetVersion.TryParse(_properties["version"], out var version))
        {
            WriteLine($"The next version has been overridden by {version}.", Color.Details);
            return version;
        }

        return _versions.GetNext(new NuGetRestoreSettings("Pure.DI"), VersionRange);
    }

    private int GetBaseTargetFrameworkMajorVersion()
    {
        var baseTargetFrameworkStr = TryGetBuildProperty("BaseTargetFramework");
        if (string.IsNullOrWhiteSpace(baseTargetFrameworkStr))
        {
            Error("Could not find the BaseTargetFramework property");
            return 0;
        }

        // parse net10.0 using regular expression
        var match = System.Text.RegularExpressions.Regex.Match(baseTargetFrameworkStr, @"^net(\d+)\.\d+$");
        // ReSharper disable once InvertIf
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out var majorVersion))
        {
            Error($"Invalid TargetFramework format: {baseTargetFrameworkStr}");
            return 0;
        }

        return majorVersion;
    }

    private string? TryGetBuildProperty(string propertyName)
    {
        var value = _buildPropsDoc.Value.Descendants(propertyName).FirstOrDefault()?.Value;
        // ReSharper disable once InvertIf
        if (string.IsNullOrWhiteSpace(value))
        {
            Error($"Could not find '{propertyName}'");
            return null;
        }

        return value;
    }

    private XDocument GetBuildPropsDoc()
    {
        var propsFile = Path.Combine(_env.GetPath(PathType.SolutionDirectory), "Directory.Build.props");
        // ReSharper disable once InvertIf
        if (!File.Exists(propsFile))
        {
            Error($"Could not find the props file: {propsFile}");
            return new XDocument();
        }

        return XDocument.Load(propsFile);
    }
}