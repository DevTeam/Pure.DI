// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBeMadeStatic.Global

namespace Build.Core;

using System.Text.RegularExpressions;
using NuGet.Versioning;

partial class Versions(INuGet nuGet)
{
    private static readonly Regex ReleaseRegex = CreateReleaseRegex();

    public NuGetVersion GetNext(NuGetRestoreSettings restoreSettings, VersionRange versionRange, int patchIncrement = 1) =>
        nuGet
            .Restore(restoreSettings.WithHideWarningsAndErrors(true).WithVersionRange(versionRange).WithNoCache(true))
            .Where(i => i.Name == restoreSettings.PackageId)
            .Select(i => i.NuGetVersion)
            .Select(i => CreateNextNuGetVersion(versionRange, patchIncrement, i))
            .Max()
        ?? CreateDefaultNuGetVersion(versionRange);

    public NuGetVersion GetCurrent(NuGetRestoreSettings restoreSettings) =>
        nuGet
            .Restore(restoreSettings.WithHideWarningsAndErrors(true).WithNoCache(true))
            .Where(i => i.Name == restoreSettings.PackageId)
            .Select(i => i.NuGetVersion)
            .Max()
        ?? CreateDefaultNuGetVersion(VersionRange.Parse("1.0.0"));

    private static NuGetVersion CreateNextNuGetVersion(VersionRange versionRange, int patchIncrement, NuGetVersion version)
    {
        return version.Release != string.Empty
            ? GetNextRelease(versionRange, version)
            : new NuGetVersion(version.Major, version.Minor, version.Patch + patchIncrement);
    }

    private static NuGetVersion CreateDefaultNuGetVersion(VersionRange versionRange) =>
        new(versionRange.MinVersion?.Major ?? 1,
            versionRange.MinVersion?.Minor ?? 0,
            versionRange.MinVersion?.Patch ?? 0);

    private static NuGetVersion GetNextRelease(VersionRangeBase versionRange, NuGetVersion version)
    {
        if (versionRange.MinVersion is null)
        {
            return version;
        }

        if (versionRange.MinVersion.Release != "0" && versionRange.MinVersion.Release != version.Release)
        {
            return versionRange.MinVersion;
        }

        var match = ReleaseRegex.Match(version.Release);
        if (!match.Success)
        {
            return version;
        }

        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (!int.TryParse(match.Groups[2].Value, out var index))
        {
            return version;
        }

        return new NuGetVersion(version.Major, version.Minor, version.Patch, match.Groups[1].Value + (index + 1));
    }

    [GeneratedRegex("""([^\d]+)([\d]*)""")]
    private static partial Regex CreateReleaseRegex();
}