// ReSharper disable MemberCanBeMadeStatic.Global
// ReSharper disable ClassNeverInstantiated.Global
namespace Build;

using System.Collections.Immutable;
using NuGet.Versioning;
using IProperties = Tools.IProperties;

[SuppressMessage("Performance", "CA1822:Mark members as static")]
internal class Settings(
    IProperties properties,
    IVersions versions)
{
    public static readonly VersionRange VersionRange = VersionRange.Parse("2.0.*");
    
    private readonly Lazy<NuGetVersion> _version = new(() => GetVersion(properties, versions));
    
    public bool BuildServer { get; } = Environment.GetEnvironmentVariable("TEAMCITY_VERSION") is not null;

    public string Configuration => "Release";

    public NuGetVersion Version => _version.Value;

    public string NuGetKey { get; } = properties["NuGetKey"];

    public ImmutableArray<CodeAnalysis> CodeAnalysis { get; } =
    [
        new CodeAnalysis(new Version(4, 3, 1))
    ];

    private static NuGetVersion GetVersion(IProperties properties, IVersions versions)
    {
        WriteLine(
            NuGetVersion.TryParse(properties["version"], out var version) 
                ? $"The version has been overridden by {version}."
                : "The next version has been used.",
            Color.Details);

        return versions.GetNext(new NuGetRestoreSettings("Pure.DI"),  VersionRange);
    }
}