// ReSharper disable MemberCanBeMadeStatic.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Build;

using System.Collections.Immutable;
using NuGet.Versioning;

[SuppressMessage("Performance", "CA1822:Mark members as static")]
internal class Settings(
    Properties properties,
    Versions versions)
{
    public static readonly VersionRange VersionRange = VersionRange.Parse("2.1.*");

    private readonly Lazy<NuGetVersion> _currentVersion = new(() => GetVersion(properties, versions));

    public bool BuildServer { get; } = Environment.GetEnvironmentVariable("TEAMCITY_VERSION") is not null;

    public string Configuration => "Release";

    public NuGetVersion CurrentVersion => _currentVersion.Value;
    
    public NuGetVersion NextVersion
    {
        get
        {
            if (NuGetVersion.TryParse(properties["version"], out var version))
            {
                WriteLine($"The next version has been overridden by {version}.", Color.Details);
                return version;
            }

            var currentVersion =  _currentVersion.Value;
            return new NuGetVersion(currentVersion.Major, currentVersion.Minor, currentVersion.Patch + 1);
        }
    }

    public string NuGetKey { get; } = properties["NuGetKey"];

    public ImmutableArray<CodeAnalysis> CodeAnalysis { get; } =
    [
        new CodeAnalysis(new Version(4, 8, 0)),
        new CodeAnalysis(new Version(4, 3, 1))
    ];

    private static NuGetVersion GetVersion(Properties properties, Versions versions) => 
        versions.GetNext(new NuGetRestoreSettings("Pure.DI"), VersionRange, 0);
}