// ReSharper disable MemberCanBeMadeStatic.Global
// ReSharper disable ClassNeverInstantiated.Global
namespace Build;

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HostApi;
using NuGet.Versioning;

[SuppressMessage("Performance", "CA1822:Mark members as static")]
internal class Settings(Properties properties)
{
    public bool BuildServer { get; } = Environment.GetEnvironmentVariable("TEAMCITY_VERSION") is not null;

    public string Configuration => "Release";

    public VersionRange VersionRange { get; } = VersionRange.Parse("2.0.*");

    public NuGetVersion? VersionOverride { get; } = GetVersionOverride(properties);

    public string NuGetKey { get; } = properties.Get("NuGetKey");

    public ImmutableArray<CodeAnalysis> CodeAnalysis { get; } =
    [
        new CodeAnalysis(new Version(4, 3, 1))
    ];

    private static NuGetVersion GetVersionOverride(Properties properties)
    {
        WriteLine(
            NuGetVersion.TryParse(properties.Get("version"), out var version) 
                ? $"The version has been overridden by {version}."
                : "The next version has been used.",
            Color.Details);

        return version;
    }
}