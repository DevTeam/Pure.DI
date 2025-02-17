// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBeMadeStatic.Global

namespace Build.Tools;

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
            .Select(i => i.Release != string.Empty
                ? GetNextRelease(versionRange, i)
                : new NuGetVersion(i.Major, i.Minor, i.Patch + patchIncrement))
            .Max()
        ?? new NuGetVersion(
            versionRange.MinVersion?.Major ?? 1,
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