namespace Build.Tools;

using NuGet.Versioning;

internal interface IVersions
{
    NuGetVersion GetNext(NuGetRestoreSettings restoreSettings, VersionRange versionRange, int patchIncrement = 1);
}