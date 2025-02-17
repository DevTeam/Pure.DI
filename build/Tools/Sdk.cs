// ReSharper disable MemberCanBeMadeStatic.Global
// ReSharper disable ReturnTypeCanBeEnumerable.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace Build.Tools;

using NuGet.Versioning;

[SuppressMessage("Performance", "CA1822:Mark members as static")]
class Sdk
{
    private readonly Lazy<IReadOnlyCollection<NuGetVersion>> _versions = new(GetVersions);

    public IEnumerable<NuGetVersion> Versions => _versions.Value;

    private static NuGetVersion[] GetVersions()
    {
        bool? getVersions = null;
        var versions = new List<NuGetVersion>();
        new DotNetCustom("--info").Run(output => {
            switch (getVersions)
            {
                case null when output.Line.Contains(".NET SDKs installed:"):
                    getVersions = true;
                    return;

                case true:
                {
                    var line = output.Line.Trim();
                    if (line == string.Empty)
                    {
                        getVersions = false;
                    }
                    else
                    {
                        var parts = line.Split('[', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 2
                            && NuGetVersion.TryParse(parts[0], out var version))
                        {
                            versions.Add(version);
                        }
                    }

                    break;
                }
            }
        });

        var sdks = versions
            .GroupBy(i => (i.Version.Major, i.Version.Minor))
            .Select(i => i.First())
            .OrderBy(i => i.Major)
            .ThenBy(i => i.Minor)
            .ToArray();

        WriteLine("Installed the .NET SDK packages:", Color.Details);
        foreach (var sdk in sdks)
        {
            WriteLine($"\t{sdk}", Color.Details);
        }

        return sdks;
    }
}