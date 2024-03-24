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

    private readonly Lazy<NuGetVersion> _version = new(() => GetVersion(properties, versions));

    public bool BuildServer { get; } = Environment.GetEnvironmentVariable("TEAMCITY_VERSION") is not null;

    public string Configuration => "Release";

    public NuGetVersion Version => _version.Value;

    public string NuGetKey { get; } = properties["NuGetKey"];

    public ImmutableArray<CodeAnalysis> CodeAnalysis { get; } =
    [
        new CodeAnalysis(new Version(4, 8, 0)),
        new CodeAnalysis(new Version(4, 3, 1))
    ];

    private static NuGetVersion GetVersion(Properties properties, Versions versions)
    {
        if (!NuGetVersion.TryParse(properties["version"], out var version))
        {
            return versions.GetNext(new NuGetRestoreSettings("Pure.DI"), VersionRange);
        }

        WriteLine($"The version has been overridden by {version}.", Color.Details);
        return version;
    }
}