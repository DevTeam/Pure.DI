// ReSharper disable HeapView.ImplicitCapture
namespace Build;

using System.Text.RegularExpressions;
using HostApi;
using NuGet.Versioning;

internal static partial class Tools
{
    private static readonly Regex ReleaseRegex = CreateReleaseRegex();
    
    public static NuGetVersion GetNextVersion(NuGetRestoreSettings settings, NuGetVersion defaultVersion)
    {
        var floatRange = defaultVersion.Release != string.Empty
            ? new FloatRange(NuGetVersionFloatBehavior.Prerelease, defaultVersion)
            : new FloatRange(NuGetVersionFloatBehavior.Minor, defaultVersion);

        return GetService<INuGet>()
            .Restore(settings.WithHideWarningsAndErrors(true).WithVersionRange(new VersionRange(defaultVersion, floatRange)))
            .Where(i => i.Name == settings.PackageId)
            .Select(i => i.NuGetVersion)
            .Select(i => defaultVersion.Release != string.Empty 
                ? new NuGetVersion(i.Major, i.Minor, i.Patch, GetNextRelease(defaultVersion.Release))
                : new NuGetVersion(i.Major, i.Minor, i.Patch + 1))
            .Max() ?? defaultVersion;
    }

    private static string GetNextRelease(string release)
    {
        var match = ReleaseRegex.Match(release);
        if (!match.Success)
        {
            return release;
        }

        if (int.TryParse(match.Groups[2].Value, out var index))
        {
            return match.Groups[1].Value + (index + 1);
        }

        return release;
    }
    
    public static string GetSolutionDirectory()
    {
        var solutionFile = TryFindFile(Environment.CurrentDirectory, "Pure.DI.sln") ?? Environment.CurrentDirectory;
        return Path.GetDirectoryName(solutionFile) ?? Environment.CurrentDirectory;
    }

    private static string? TryFindFile(string? path, string searchPattern)
    {
        string? target = default;
        while (path != default && target == default)
        {
            target = Directory.EnumerateFileSystemEntries(path, searchPattern).FirstOrDefault();
            path = Path.GetDirectoryName(path);
        }

        return target;
    }

    [GeneratedRegex("""([^\d]+)([\d]*)""")]
    private static partial Regex CreateReleaseRegex();
}