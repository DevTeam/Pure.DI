namespace Build;

using HostApi;
using NuGet.Versioning;

internal static class Tools
{
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
                ? new NuGetVersion(i.Major, i.Minor, i.Patch, defaultVersion.Release)
                : new NuGetVersion(i.Major, i.Minor, i.Patch + 1))
            .Max() ?? defaultVersion;
    }

    public static void CheckRequiredSdk(Version requiredSdkVersion)
    {
        Version? sdkVersion = default;
        // ReSharper disable once InvertIf
        if (
            new DotNetCustom("--version")
                .WithShortName($"Checking the .NET SDK version {requiredSdkVersion}")
#pragma warning disable CA1806
                .Run(output=> Version.TryParse(output.Line, out sdkVersion)) == 0
#pragma warning restore CA1806
            && sdkVersion != requiredSdkVersion)
        {
            Error($".NET SDK {requiredSdkVersion} is required.");
            Environment.Exit(1);
        }
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
}